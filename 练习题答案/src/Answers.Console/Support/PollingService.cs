using Answers.ConsoleApp.Models;
using Answers.ConsoleApp.Modbus;

namespace Answers.ConsoleApp.Services;

/// <summary>一轮采集完成后的快照。真实项目会把它转换成带质量、报警的位号值列表。</summary>
public sealed record PollingSnapshot(string DeviceName, ushort[] Registers);

/// <summary>
/// 精简版采集引擎，只保留理解“周期采集”所需的骨架：
/// - Start 在后台启动循环，不阻塞界面；
/// - 每个周期调用一次主站读寄存器；
/// - 每次读完后触发 SnapshotReady，界面/存盘模块订阅它；
/// - StopAsync 负责取消并等待后台循环退出。
///
/// 与主项目 Industrial.Core 的完整版相比，这里省略了多点位合并、质量判断、报警等内容，
/// 目的是让第 07 章练习 5 能独立运行、只观察“配置周期”和“实际间隔”的关系。
/// </summary>
public sealed class PollingService
{
    private readonly AppConfig _config;
    private readonly Func<DeviceConfig, IModbusMaster> _masterFactory;

    private CancellationTokenSource? _cts;
    private Task? _worker;

    public PollingService(AppConfig config, Func<DeviceConfig, IModbusMaster> masterFactory)
    {
        _config = config;
        _masterFactory = masterFactory;
    }

    /// <summary>每完成一轮采集触发一次。事件可能在后台线程触发，订阅方要自己保证线程安全。</summary>
    public event Action<PollingSnapshot>? SnapshotReady;

    /// <summary>启动后台采集循环，立即返回。</summary>
    public void Start()
    {
        if (_worker is not null) throw new InvalidOperationException("采集已经启动。");

        _cts = new CancellationTokenSource();
        _worker = Task.Run(() => RunAsync(_cts.Token));
    }

    /// <summary>请求停止，并等待后台循环真正结束。</summary>
    public async Task StopAsync()
    {
        if (_cts is null) return;

        _cts.Cancel();
        if (_worker is not null)
        {
            try { await _worker; }
            catch (OperationCanceledException) { /* 正常停止 */ }
        }

        _cts.Dispose();
        _cts = null;
        _worker = null;
    }

    /// <summary>后台循环：连接 → 按周期读 → 触发快照，直到被取消。</summary>
    private async Task RunAsync(CancellationToken ct)
    {
        var device = _config.Devices.FirstOrDefault();
        if (device is null) return;

        using var master = _masterFactory(device);
        await master.ConnectAsync(ct);

        // 至少 1 个寄存器，避免 count = 0 的非法请求。
        var count = (ushort)Math.Max(1, device.Points.Count);

        // PeriodicTimer 由计时器维护节拍，避免“干完活再 Delay”造成的累积漂移。
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(Math.Max(10, device.PollIntervalMs)));

        while (await timer.WaitForNextTickAsync(ct))
        {
            var registers = await master.ReadHoldingRegistersAsync(device.SlaveId, 0, count, ct);
            SnapshotReady?.Invoke(new PollingSnapshot(device.Name, registers));
        }
    }
}
