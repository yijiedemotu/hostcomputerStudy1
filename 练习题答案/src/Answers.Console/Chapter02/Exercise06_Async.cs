using System.Diagnostics;

namespace Answers.ConsoleApp;

/// <summary>
/// 第 02 章 · 练习 6：写一个 async Task&lt;int&gt; 方法，1 秒后返回 42。
///
/// 题目：GetAnswerAsync 延迟 1 秒返回 42，在控制台用 await 等待并打印耗时。
///
/// 为什么上位机要学这个：
/// 读串口、等网络应答、查数据库都要时间。如果界面线程一直“干等”，窗口就会卡住。
/// async/await 让方法在等待时把线程让出去，界面仍能响应点击，这在采集软件里是基本功。
/// </summary>
internal static class Exercise06_Async
{
    public static async Task RunAsync()
    {
        ConsoleUi.Section("练习 6：async Task<int>，1 秒后返回 42，并在控制台 await");

        // Stopwatch 是秒表，用来测量一段代码实际花了多久。
        // StartNew() 创建并立即开始计时。
        var sw = Stopwatch.StartNew();
        Console.WriteLine("    调用 GetAnswerAsync() ...");

        // await 的作用：
        // 1. 调用 GetAnswerAsync() 会立刻返回一个 Task<int>（可以理解为“未来会有结果的任务”）；
        // 2. await 等待任务完成，期间不阻塞线程，控制台主线程可以去干别的；
        // 3. 任务完成后，把 int 结果取出来赋给 answer。
        var answer = await GetAnswerAsync();

        sw.Stop();
        ConsoleUi.KeyValue("返回值", answer);
        ConsoleUi.KeyValue("实际耗时", $"{sw.ElapsedMilliseconds} ms");
        ConsoleUi.Note("await 不会阻塞线程；控制台主线程在等待期间可以让出，和 .Result 阻塞有本质区别。");
    }

    /// <summary>
    /// 异步方法：返回类型写 Task&lt;int&gt;，表示“将来会给出一个 int”。
    /// 方法内部可以用 await；调用方也必须 await（或显式处理 Task）。
    /// </summary>
    private static async Task<int> GetAnswerAsync()
    {
        // Task.Delay(1000) 表示“异步等待 1 秒”。
        // 它和 Thread.Sleep(1000) 的区别：Sleep 会把当前线程硬生生卡住，Delay 不会。
        await Task.Delay(1000);

        // return 42 会被自动包装进 Task<int> 返回给调用方。
        return 42;
    }
}
