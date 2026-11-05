using System.Diagnostics;
using System.Threading.Channels;
using Answers.ConsoleApp.Models;

namespace Answers.ConsoleApp;

/// <summary>
/// 第 10 章 · 练习 2：用 Channel 解耦“采集 100ms、批量存盘 1s”。
///
/// 题目场景：
/// 采集端每 100ms 产生一条历史数据，存盘端想每秒批量写一次磁盘。
/// 如果让采集线程直接写磁盘，磁盘一慢，采集就被拖住，周期立刻失控。
///
/// 解决思路：在两者之间放一个 Channel（线程安全的异步队列）。
/// - 采集端只负责 WriteAsync，写完就走；
/// - 存盘端按自己的节奏 ReadAllAsync，攒够/到时间再批量落盘；
/// - 队列设上限 1000，满了丢最旧的数据（DropOldest），保护内存。
///
/// 为什么上位机要学这个：
/// 采集、显示、存盘、报警计算的速度差异很大。用队列隔开，慢的一方不会把快的一方拖死。
/// </summary>
internal static class Exercise02_ChannelDecoupling
{
    public static async Task RunAsync()
    {
        ConsoleUi.Section("练习 2：Channel 解耦“采集 100ms、批量存盘 1s”");

        // CreateBounded：有界队列，最多放 1000 条。
        // FullMode = DropOldest：队列满时丢最旧的记录，而不是让生产者卡住或把内存吃光。
        // SingleReader / SingleWriter：告诉运行时只有一个消费者、一个生产者，可以走更快的优化路径。
        var channel = Channel.CreateBounded<HistoryRecord>(new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = true
        });

        var produced = 0;
        var consumed = 0;
        var batchSizes = new List<int>();

        // 消费者：后台任务，负责批量“存盘”（这里用内存列表模拟）。
        var consumer = Task.Run(async () =>
        {
            var batch = new List<HistoryRecord>();
            var batchClock = Stopwatch.StartNew();

            // ReadAllAsync 会一直等新数据；生产者调用 Complete 后，读完剩余数据就结束循环。
            await foreach (var record in channel.Reader.ReadAllAsync())
            {
                batch.Add(record);

                // 攒了 1 秒就写一批。真实代码这里会调用 CSV/数据库的批量插入。
                if (batchClock.ElapsedMilliseconds >= 1000)
                {
                    batchSizes.Add(batch.Count);
                    consumed += batch.Count;
                    batch.Clear();
                    batchClock.Restart();
                }
            }

            // 生产者结束后，把最后不足 1 秒的一批也写掉，避免丢尾巴。
            if (batch.Count > 0)
            {
                batchSizes.Add(batch.Count);
                consumed += batch.Count;
            }
        });

        // 生产者：每 100ms 产生一条记录，共 30 条（约 3 秒）。
        var producer = Task.Run(async () =>
        {
            for (var i = 0; i < 30; i++)
            {
                var record = new HistoryRecord(
                    DateTime.Now, "1#炉", $"温度{i % 3}", 80 + i * 0.1, "℃", Quality.Good, AlarmLevel.None);

                await channel.Writer.WriteAsync(record);
                produced++;
                await Task.Delay(100);
            }

            // 告诉消费者“不会再有新数据了”。不调用 Complete，消费者的 await foreach 会永远等下去。
            channel.Writer.Complete();
        });

        // 等生产和消费都结束。
        await Task.WhenAll(producer, consumer);

        ConsoleUi.KeyValue("生产 / 消费", $"{produced} 条 / {consumed} 条");
        ConsoleUi.KeyValue("批量写盘批次大小", string.Join("、", batchSizes));
        ConsoleUi.Ok("采集循环只负责写 Channel，存盘慢不会阻塞采集；队列有界，满了丢最旧数据而不是吃光内存。");
        ConsoleUi.Note("生产代码在 Channel 满时要明确策略：DropOldest / DropWrite / 阻塞等待，并记录丢弃计数。");
    }
}
