using System.Collections.Concurrent;

namespace Answers.ConsoleApp;

/// <summary>
/// 第 10 章 · 练习 3：故意写一个 .Result 死锁，再给出两种修复。
///
/// 题目：在“UI 线程”上调用 GetValueAsync().Result，观察窗口/线程卡死；然后改成正确写法。
///
/// 死锁是怎么发生的：
/// 1. UI 线程执行 GetValueAsync().Result，会阻塞在这里等结果；
/// 2. GetValueAsync 里的 await 默认会捕获当前的 SynchronizationContext（UI 上下文），
///    并计划在“UI 线程”上继续执行后面代码；
/// 3. 但 UI 线程正被 .Result 卡住，没法去执行那个后续动作；
/// 4. 两边互相等待 → 死锁。
///
/// 两种修复：
/// 1. 全链路 await，不阻塞 UI 线程；
/// 2. 库代码里用 ConfigureAwait(false)，后续代码不要求回 UI 线程，在线程池执行即可。
///
/// 注意：本练习用一个后台线程 + 假 SynchronizationContext 复现，卡死也不会阻止程序退出。
/// 在真实 WinForms/WPF 里，表现就是整个窗口无响应。
/// </summary>
internal static class Exercise03_Deadlock
{
    public static async Task RunAsync()
    {
        ConsoleUi.Section("练习 3：故意写一个 .Result 死锁，再改对");

        // 创建假的 UI 上下文，并启动一个后台线程专门“扮演 UI 线程”。
        var context = new SingleThreadSynchronizationContext();
        var fakeUi = new Thread(() =>
        {
            // 让当前线程拥有这个上下文，之后 await 就会把后续动作排到它上面。
            SynchronizationContext.SetSynchronizationContext(context);
            context.RunOnCurrentThread();
        })
        {
            IsBackground = true,     // 后台线程：卡死也不阻止程序退出
            Name = "假 UI 线程"
        };
        fakeUi.Start();

        // 把一段“会死锁的代码”投递到假 UI 线程执行。
        context.Post(_ =>
        {
            Console.WriteLine("    [假 UI 线程] 正在执行 GetValueAsync().Result ...");

            // .Result 会同步阻塞，直到任务完成。问题就在这里。
            var value = GetValueAsync().Result;

            // 下面这行永远不会执行，因为上面的 .Result 已经死锁了。
            Console.WriteLine($"    [假 UI 线程] 拿到结果 {value}");
        }, null);

        // 主线程等 1.5 秒，确认假 UI 线程没有任何输出。
        await Task.Delay(1500);
        ConsoleUi.Error("1.5 秒后仍没有输出：.Result 死锁发生了。");
        ConsoleUi.Answer("原因：GetValueAsync 里的 await 捕获了假 UI 线程的 SynchronizationContext；" +
                         ".Result 把该线程阻塞住，continuation 想回 UI 线程却永远排不上队。");
        ConsoleUi.Note("这个演示线程是后台线程，卡住也不会阻止程序退出；真实 WinForms/WPF 里就是整个窗口卡死。");

        // 修复 1：调用方也用 await，UI 线程在等待期间可以继续处理消息，不会被堵死。
        var fixedValue = await GetValueAsync();
        ConsoleUi.KeyValue("修复 1：全链路 await", fixedValue);

        // 修复 2：方法内部 ConfigureAwait(false)，后续代码不要求回原上下文。
        // 此时即使调用方用 GetResult() 同步等待，也不会死锁。
        var noCapture = GetValueNoCaptureAsync().GetAwaiter().GetResult();
        ConsoleUi.KeyValue("修复 2（ConfigureAwait(false) 后 GetResult）", noCapture);
        ConsoleUi.Note("UI 事件处理器里永远不要 .Result / .Wait()；库代码用 ConfigureAwait(false)，UI 代码用 await。");
    }

    /// <summary>
    /// 普通异步方法：await 之后默认要回到原来的上下文（UI 线程）。
    /// 在 UI 线程上同步等待它，就会死锁。
    /// </summary>
    private static async Task<int> GetValueAsync()
    {
        await Task.Delay(100);
        return 42;
    }

    /// <summary>
    /// 修复版：ConfigureAwait(false) 表示“后续代码不必回 UI 线程”，
    /// 线程池随便找个线程继续执行即可，因此不会被 UI 线程的阻塞卡住。
    /// </summary>
    private static async Task<int> GetValueNoCaptureAsync()
    {
        await Task.Delay(100).ConfigureAwait(false);
        return 42;
    }

    /// <summary>
    /// 一个极简单线程 SynchronizationContext：
    /// Post 把回调放进队列，RunOnCurrentThread 在同一个线程里依次执行。
    /// 它模拟了 WinForms/WPF 的“所有 UI 操作都在同一线程排队”的行为。
    /// </summary>
    private sealed class SingleThreadSynchronizationContext : SynchronizationContext
    {
        // BlockingCollection 是线程安全的阻塞队列：没有元素时取数据会等待。
        private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> _queue = new();

        public override void Post(SendOrPostCallback d, object? state) => _queue.Add((d, state));

        public override void Send(SendOrPostCallback d, object? state) =>
            throw new NotSupportedException("演示上下文只支持 Post，不支持 Send。");

        public void RunOnCurrentThread()
        {
            // GetConsumingEnumerable 会不断取元素，直到队列被 Complete。
            foreach (var (callback, state) in _queue.GetConsumingEnumerable())
                callback(state);
        }
    }
}
