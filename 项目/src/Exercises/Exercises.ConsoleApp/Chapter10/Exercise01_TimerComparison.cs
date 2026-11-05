using System.Diagnostics;

namespace Exercises.ConsoleApp;

/// <summary>
/// 第 10 章 · 练习 1：比较 PeriodicTimer 和 Task.Delay 的周期精度。
///
/// 题目：设定 100ms 周期、每次工作 40ms，分别用三种写法跑约 2.2 秒，
/// 统计实际间隔的平均值、最小值、最大值和抖动。
///
/// 三种写法：
/// 1. Task.Delay（不补偿）：干完活后再等 100ms，实际周期 = 40 + 100 = 140ms，越跑越偏；
/// 2. Task.Delay（扣耗时补偿）：开工时记时间，干完活只补剩下的时间，周期接近 100ms；
/// 3. PeriodicTimer：由计时器自己维护节拍，即使某次晚了，下一拍仍按原时间表走，不累积漂移。
///
/// 为什么上位机要学这个：
/// 采集周期直接决定趋势图的密度、历史数据的间隔和总线负载。
/// 用错写法会产生“越采越慢”的假象，现场排查时很容易误判成设备变慢。
/// </summary>
internal static class Exercise01_TimerComparison
{
    public static async Task RunAsync()
    {
        ConsoleUi.Section("练习 1：PeriodicTimer vs Task.Delay，比较周期精度");

        var period = TimeSpan.FromMilliseconds(100);   // 期望每 100ms 采一次
        var work = TimeSpan.FromMilliseconds(40);      // 每次采集本身要花 40ms
        var duration = TimeSpan.FromSeconds(2.2);      // 每种写法观察 2.2 秒

        var naive = await RunNaiveDelayAsync(period, work, duration);
        var compensated = await RunCompensatedDelayAsync(period, work, duration);
        var periodic = await RunPeriodicTimerAsync(period, work, duration);

        // 打印一张简单的对比表。
        Console.WriteLine();
        Console.WriteLine("    方案                         次数   平均间隔   最小     最大     抖动(标准差)");
        Console.WriteLine("    " + new string('-', 76));
        Print("Task.Delay（不补偿）", naive);
        Print("Task.Delay（扣耗时补偿）", compensated);
        Print("PeriodicTimer", periodic);
        Console.WriteLine();

        ConsoleUi.Answer("PeriodicTimer 的周期由计时器自己维护，采集耗时不会累积成漂移；" +
                         "Task.Delay 不补偿时，实际周期 = 周期 + 采集耗时。");
        ConsoleUi.Note("如果一次采集超过周期，PeriodicTimer 会合并错过的 tick，不会把下一次立即补跑成一串；" +
                       "本项目 PollingService 用 Stopwatch 扣耗时，也是为了避免漂移。");
    }

    /// <summary>把一次测量的结果打印成表格的一行。</summary>
    private static void Print(string name, ScheduleResult result)
    {
        // {name,-28} 表示左对齐占 28 个字符；{result.Count,3} 表示右对齐占 3 个字符。
        Console.WriteLine($"    {name,-28} {result.Count,3}   {result.Average,8:F1}ms {result.Min,7:F1}ms {result.Max,7:F1}ms {result.Jitter,9:F1}ms");
    }

    /// <summary>
    /// 写法 1：干完 40ms 的活，再固定等 100ms。
    /// 错误点：等待时间没有扣掉干活时间，所以真实间隔约 140ms。
    /// </summary>
    private static async Task<ScheduleResult> RunNaiveDelayAsync(TimeSpan period, TimeSpan work, TimeSpan duration)
    {
        var intervals = new List<double>();       // 保存每两次循环之间的间隔
        var sw = Stopwatch.StartNew();            // 秒表，记录整个实验的时间
        double? last = null;                      // 上一次循环开始的时间点

        while (sw.Elapsed < duration)
        {
            var now = sw.Elapsed.TotalMilliseconds;
            if (last is not null) intervals.Add(now - last.Value);
            last = now;

            await Task.Delay(work);               // 模拟采集耗时
            await Task.Delay(period);             // 再固定等一个周期（问题就在这里）
        }
        return Summarize(intervals);
    }

    /// <summary>
    /// 写法 2：记录本轮开始时间，干完活后只补“周期 - 已用时间”。
    /// 这样 40ms 干活 + 60ms 等待 ≈ 100ms，周期基本稳定。
    /// </summary>
    private static async Task<ScheduleResult> RunCompensatedDelayAsync(TimeSpan period, TimeSpan work, TimeSpan duration)
    {
        var intervals = new List<double>();
        var sw = Stopwatch.StartNew();
        double? last = null;

        while (sw.Elapsed < duration)
        {
            var start = sw.Elapsed.TotalMilliseconds;
            if (last is not null) intervals.Add(start - last.Value);
            last = start;

            await Task.Delay(work);

            // 还剩多少时间才到下一个周期？如果干活已经超时，wait 会是负数，就不再等。
            var wait = period.TotalMilliseconds - (sw.Elapsed.TotalMilliseconds - start);
            if (wait > 1) await Task.Delay((int)wait);
        }
        return Summarize(intervals);
    }

    /// <summary>
    /// 写法 3：PeriodicTimer 自带节拍。
    /// WaitForNextTickAsync 每次等到下一个理论节拍点，上一拍耗时不会顺延到下一拍。
    /// </summary>
    private static async Task<ScheduleResult> RunPeriodicTimerAsync(TimeSpan period, TimeSpan work, TimeSpan duration)
    {
        var intervals = new List<double>();
        var sw = Stopwatch.StartNew();
        double? last = null;

        // PeriodicTimer 由 .NET 的计时器驱动，using 结束时自动释放。
        using var timer = new PeriodicTimer(period);

        // 2.2 秒后取消等待，退出 while。
        using var cts = new CancellationTokenSource(duration);
        try
        {
            while (await timer.WaitForNextTickAsync(cts.Token))
            {
                var now = sw.Elapsed.TotalMilliseconds;
                if (last is not null) intervals.Add(now - last.Value);
                last = now;

                await Task.Delay(work, cts.Token);   // 模拟采集耗时
            }
        }
        catch (OperationCanceledException)
        {
            // 到时间后 WaitForNextTickAsync 会抛这个异常，属于正常退出，不用处理。
        }
        return Summarize(intervals);
    }

    /// <summary>
    /// 统计间隔数据：平均、最小、最大、标准差（抖动）。
    /// 标准差越大，说明周期越不稳定。
    /// </summary>
    private static ScheduleResult Summarize(List<double> intervals)
    {
        if (intervals.Count == 0) return new ScheduleResult(0, 0, 0, 0, 0);

        var avg = intervals.Average();
        var variance = intervals.Sum(v => (v - avg) * (v - avg)) / intervals.Count;
        return new ScheduleResult(intervals.Count, avg, intervals.Min(), intervals.Max(), Math.Sqrt(variance));
    }

    /// <summary>一次周期实验的统计结果。</summary>
    private sealed record ScheduleResult(int Count, double Average, double Min, double Max, double Jitter);
}
