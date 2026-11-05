using Answers.ConsoleApp.Modbus;
using Answers.ConsoleApp.Models;
using Answers.ConsoleApp.Services;

namespace Answers.ConsoleApp;

/// <summary>
/// 第 07 章 · 练习 5：把采集周期改成 100ms，观察快照节奏。
///
/// 题目：修改 PollingService 的采集周期，记录一段时间内产生多少快照，比较实际间隔。
///
/// 结论先看：
/// - 100ms 周期能跑，但 485/9600 下一帧往往就要 50~80ms，还要算从站处理时间；
/// - 周期低于一帧耗时，会导致总线拥堵、偶发超时；
/// - PC 的 CPU 通常不是瓶颈，别被任务管理器骗了。
/// 现场推荐 200~1000ms；点位多时先合并批量读，再考虑缩短周期。
///
/// 为了不依赖模拟器，本练习用一个“固定延迟 35ms 的假主站”代替真实设备，
/// 这样每次运行都能稳定比较 100ms 和 500ms 两种配置。
/// </summary>
internal static class Exercise05_Polling100ms
{
    public static async Task RunAsync()
    {
        ConsoleUi.Section("练习 5：把 PollingService 采集周期改成 100ms，观察快照节奏");

        ConsoleUi.Note("这里用带 35ms 延迟的仿真主站，避免依赖模拟器；真实环境请改 appsettings.json 的 PollIntervalMs 并观察模拟器 --verbose 日志和 CPU。");

        // 分别用 100ms 和 500ms 跑一遍，每遍约 2.2 秒。
        var fast = await MeasurePollingAsync(100);
        var normal = await MeasurePollingAsync(500);

        // 平均间隔 = 相邻两次快照时间差的平均值。
        ConsoleUi.KeyValue("配置 100ms", $"快照 {fast.Count} 个，平均间隔 {fast.AverageInterval:F1} ms");
        ConsoleUi.KeyValue("配置 500ms", $"快照 {normal.Count} 个，平均间隔 {normal.AverageInterval:F1} ms");
        ConsoleUi.Answer("100ms 周期能跑起来，但 485/9600 下一帧往往就要 50~80ms，且从站需要处理时间；" +
                         "采集周期低于一帧耗时会导致总线拥堵、偶发超时。 PC 的 CPU 通常不是瓶颈，别被任务管理器骗了。");
        ConsoleUi.Note("现场推荐 200~1000ms；点位多时先批量合并读，再考虑缩短周期。");
    }

    /// <summary>
    /// 用指定周期运行一次采集，返回快照数量和平均间隔。
    /// </summary>
    private static async Task<(int Count, double AverageInterval)> MeasurePollingAsync(int intervalMs)
    {
        // AppConfig 就是 appsettings.json 对应的对象。
        // 这里用对象初始化器直接构造：一个设备，三个点位。
        var config = new AppConfig
        {
            Devices =
            {
                new DeviceConfig
                {
                    Name = "仿真设备",
                    Protocol = DeviceProtocol.ModbusTcp,
                    Host = "127.0.0.1",
                    Port = 502,
                    SlaveId = 1,
                    PollIntervalMs = intervalMs,   // ← 本题要比较的就是这个值
                    Points =
                    {
                        new PointConfig { Name = "温度", Address = 0, DataType = RegisterDataType.Int16, Scale = 0.1, Unit = "℃" },
                        new PointConfig { Name = "压力", Address = 1, DataType = RegisterDataType.UInt16, Scale = 0.01, Unit = "MPa" },
                        new PointConfig { Name = "转速", Address = 2, DataType = RegisterDataType.UInt16, Unit = "rpm" }
                    }
                }
            }
        };

        // 记录每次“一轮采集完成”的时间戳。
        var stamps = new List<DateTime>();

        // 第二个参数是“主站工厂”：PollingService 每次需要连接时调用它创建主站。
        // 这里返回假主站，避免真的去连 502 端口。
        var service = new PollingService(config, _ => new LatencyMaster(35));

        // SnapshotReady 是采集完成事件。每完成一轮，就把当前时间加进列表。
        // lock 是因为事件可能在后台线程触发，而主线程稍后要读这个列表，防止同时读写冲突。
        service.SnapshotReady += _ =>
        {
            lock (stamps) stamps.Add(DateTime.Now);
        };

        service.Start();
        await Task.Delay(2200);          // 观察 2.2 秒
        await service.StopAsync();       // 停止后台采集循环，等它收尾

        List<DateTime> snapshot;
        lock (stamps) snapshot = stamps.ToList();

        // Zip + Skip 的写法：把“每个时间”和“下一个时间”配对，算出相邻间隔。
        // 例如 [t0,t1,t2] 与 [t1,t2] 配对，得到 (t1-t0) 和 (t2-t1)。
        var average = snapshot.Count > 1
            ? snapshot.Zip(snapshot.Skip(1), (a, b) => (b - a).TotalMilliseconds).Average()
            : 0;
        return (snapshot.Count, average);
    }

    /// <summary>
    /// 假主站：不连网络，只等待固定延迟后返回一组递增的寄存器值。
    /// 用它模拟“一次采集本身要花 35ms”，让周期对比更接近真实现场。
    /// </summary>
    private sealed class LatencyMaster(int latencyMs) : IModbusMaster
    {
        private int _tick;

        public bool IsConnected => true;
        public string Endpoint => "仿真设备";

        public Task ConnectAsync(CancellationToken ct = default) => Task.CompletedTask;
        public void Disconnect() { }

        public async Task<ushort[]> ReadHoldingRegistersAsync(byte unitId, ushort startAddress, ushort count, CancellationToken ct = default)
        {
            // 模拟网络/从站处理耗时。
            await Task.Delay(latencyMs, ct);

            // Interlocked.Increment 是线程安全的自增，多个采集线程同时调用也不会算错。
            var n = Interlocked.Increment(ref _tick);

            // 返回 n, n+1, n+2 ... 这样每个快照的数值都不一样，方便观察。
            return Enumerable.Range(0, count).Select(i => (ushort)(n + i)).ToArray();
        }

        // 其余接口本题用不到，直接抛“不支持”，避免误调用。
        public Task<bool[]> ReadCoilsAsync(byte unitId, ushort startAddress, ushort count, CancellationToken ct = default)
            => throw new NotSupportedException();
        public Task<bool[]> ReadDiscreteInputsAsync(byte unitId, ushort startAddress, ushort count, CancellationToken ct = default)
            => throw new NotSupportedException();
        public Task<ushort[]> ReadInputRegistersAsync(byte unitId, ushort startAddress, ushort count, CancellationToken ct = default)
            => throw new NotSupportedException();
        public Task WriteSingleCoilAsync(byte unitId, ushort address, bool value, CancellationToken ct = default)
            => throw new NotSupportedException();
        public Task WriteSingleRegisterAsync(byte unitId, ushort address, ushort value, CancellationToken ct = default)
            => throw new NotSupportedException();
        public Task WriteMultipleRegistersAsync(byte unitId, ushort startAddress, ushort[] values, CancellationToken ct = default)
            => throw new NotSupportedException();
        public void Dispose() { }
    }
}
