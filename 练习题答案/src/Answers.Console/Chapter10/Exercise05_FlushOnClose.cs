using System.Threading.Channels;
using Answers.ConsoleApp.Models;

namespace Answers.ConsoleApp;

/// <summary>
/// 第 10 章 · 练习 5：关闭程序前，把队列里的历史数据全部落盘。
///
/// 题目：采集线程把数据写进队列，存盘线程批量写入（故意慢启动）。
/// 如果直接关进程，队列里没写完的数据就丢了。要求关闭时先 Flush，再等存盘线程结束。
///
/// 正确关闭顺序：
/// 1. 生产者停止写入；
/// 2. 调用 channel.Writer.Complete()，告诉消费者“不会再有新数据”；
/// 3. await 消费者任务，等它把剩余批次写完；
/// 4. 最后才退出程序。
///
/// 为什么上位机要学这个：
/// 历史数据是追溯事故、复盘的依据。关闭时丢最后几秒数据，现场往往很在意。
/// </summary>
internal static class Exercise05_FlushOnClose
{
    public static async Task RunAsync()
    {
        ConsoleUi.Section("练习 5：关闭时保证队列里的历史数据全部落盘");

        // 每批 10 条、批量写盘耗时 60ms、消费者启动慢 300ms。
        // 慢启动是为了模拟磁盘/数据库繁忙，让“刚写完时还没落盘”的现象更明显。
        var writer = new BufferedHistoryWriter(batchSize: 10, batchDelayMs: 60, startupDelayMs: 300);
        writer.Start();

        // 连续写 50 条历史记录。
        for (var i = 1; i <= 50; i++)
        {
            await writer.WriteAsync(new HistoryRecord(
                DateTime.Now, "1#炉", $"温度{i % 3}", 80 + i * 0.1, "℃", Quality.Good, AlarmLevel.None));
        }

        // 这时消费者可能还没开始工作，所以已落盘数量通常小于 50。
        ConsoleUi.KeyValue("刚写完 50 条时", $"已落盘 {writer.WrittenCount} 条，待处理约 {writer.PendingCount} 条");
        ConsoleUi.Note("消费者故意慢启动，模拟磁盘/数据库慢；如果此时直接关进程，队列中的历史数据就丢了。");

        // FlushAsync 内部：Complete + 等待消费者把剩余数据写完。
        await writer.FlushAsync();

        ConsoleUi.KeyValue("Complete + await 之后", $"已落盘 {writer.WrittenCount}/50 条");
        if (writer.WrittenCount == 50)
            ConsoleUi.Ok("关闭前调用 FlushHistory/Complete 并等待消费线程结束，最后一批数据也保住了。");
        else
            ConsoleUi.Error("仍有数据未落盘，检查 Flush 是否等待了消费者。");
    }

    /// <summary>
    /// 带队列的存盘器：
    /// - 生产者只管 WriteAsync，不直接碰磁盘；
    /// - 消费者在后台批量写盘；
    /// - FlushAsync 负责“关门 + 等最后一批写完”。
    /// </summary>
    private sealed class BufferedHistoryWriter
    {
        // 无界队列：本题数据量小，不需要限流。
        // 真实项目若数据量不可控，应改成有界队列并制定丢弃策略。
        private readonly Channel<HistoryRecord> _channel = Channel.CreateUnbounded<HistoryRecord>();

        // 已落盘的数据。真实项目这里会是 CSV 文件或数据库表。
        private readonly List<HistoryRecord> _written = new();

        // 保护 _written 的锁：写盘在后台线程，读取计数在主线程。
        private readonly object _gate = new();

        private readonly int _batchSize;
        private readonly int _batchDelayMs;
        private readonly int _startupDelayMs;
        private Task? _worker;

        public BufferedHistoryWriter(int batchSize, int batchDelayMs, int startupDelayMs)
        {
            _batchSize = batchSize;
            _batchDelayMs = batchDelayMs;
            _startupDelayMs = startupDelayMs;
        }

        /// <summary>已落盘条数（线程安全地读取）。</summary>
        public int WrittenCount
        {
            get { lock (_gate) return _written.Count; }
        }

        /// <summary>还在队列里、尚未落盘的条数。</summary>
        public int PendingCount => Volatile.Read(ref _pending);

        private int _pending;

        /// <summary>启动后台消费任务（不阻塞调用方）。</summary>
        public void Start() => _worker = Task.Run(ConsumeAsync);

        /// <summary>生产者写入一条记录。先增加待处理计数，再入队。</summary>
        public async ValueTask WriteAsync(HistoryRecord record)
        {
            Interlocked.Increment(ref _pending);
            await _channel.Writer.WriteAsync(record);
        }

        /// <summary>
        /// 关闭入口：先 Complete 告诉消费者没有新数据，再 await 消费者结束。
        /// await 返回时，队列里的数据已经全部落盘。
        /// </summary>
        public async Task FlushAsync()
        {
            _channel.Writer.Complete();
            if (_worker is not null) await _worker;
        }

        /// <summary>后台消费者：慢启动 → 不断取数据 → 每满一批就写盘 → 最后写尾巴。</summary>
        private async Task ConsumeAsync()
        {
            await Task.Delay(_startupDelayMs); // 模拟磁盘/数据库慢启动

            var batch = new List<HistoryRecord>(_batchSize);

            // 读完所有数据后循环结束（Complete 之后且队列为空）。
            await foreach (var record in _channel.Reader.ReadAllAsync())
            {
                batch.Add(record);

                // 从“待处理”里减 1：Interlocked 保证多线程下计数准确。
                Interlocked.Decrement(ref _pending);

                if (batch.Count >= _batchSize)
                {
                    await FlushBatchAsync(batch);
                    batch.Clear();
                }
            }

            // 最后不满一批的也要写掉，否则关闭时会丢掉尾巴。
            if (batch.Count > 0)
                await FlushBatchAsync(batch);
        }

        /// <summary>模拟一次批量写盘，并把数据记到 _written。</summary>
        private async Task FlushBatchAsync(List<HistoryRecord> batch)
        {
            await Task.Delay(_batchDelayMs); // 模拟一次批量写盘

            // lock 防止主线程在读取 WrittenCount 时与这里同时修改 List。
            lock (_gate) _written.AddRange(batch);
        }
    }
}
