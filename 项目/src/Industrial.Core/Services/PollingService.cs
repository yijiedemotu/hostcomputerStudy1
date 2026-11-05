using System.Diagnostics;
using Industrial.Core.Diagnostics;
using Industrial.Core.Models;
using Industrial.Core.Modbus;

namespace Industrial.Core.Services;

/// <summary>
/// 采集引擎：每台设备一个后台循环，按周期读寄存器 → 解析工程值 → 判断报警 → 抛事件。
///
/// 这里体现了上位机最关键的三个设计原则：
///  1. **采集与界面分离**：采集在后台 Task 里跑，UI 只订阅 <see cref="SnapshotReady"/>，
///     通信卡死也不会把界面拖成“未响应”；
///  2. **批量读**：把地址相邻的点位合并成一次 03 请求（上限 125 个寄存器），
///     否则 20 个点就是 20 帧，485 总线根本扛不住；
///  3. **故障可视化**：通信失败不是静默重试，而是把该设备所有点打成 Bad 质量上报，
///     界面上直接显示“故障”，避免操作工看着过期数据做决策。
/// </summary>
public sealed class PollingService : IAsyncDisposable
{
    private const int MaxRegistersPerRead = 125;

    private readonly AppConfig _config;
    private readonly Func<DeviceConfig, IModbusMaster> _factory;
    private CancellationTokenSource? _cts;
    private Task[] _loops = Array.Empty<Task>();

    /// <summary>
    /// 构造采集服务。
    /// factory 可以传入自定义主站创建方法（单元测试时用来塞假主站）；
    /// 不传就使用默认的 ModbusMasterFactory。
    /// </summary>
    public PollingService(AppConfig config, Func<DeviceConfig, IModbusMaster>? factory = null)
    {
        _config = config;
        _factory = factory ?? (d => ModbusMasterFactory.Create(d));
    }

    /// <summary>一批采集结果（同一台设备、同一时刻）。回调在后台线程，UI 必须 Invoke/BeginInvoke。</summary>
    public event Action<IReadOnlyList<TagValue>>? SnapshotReady;

    /// <summary>设备状态变化：参数 1 为描述文字，参数 2 表示是否在线。</summary>
    public event Action<string, bool>? DeviceStateChanged;

    public bool IsRunning => _cts is not null;
    public AppConfig Config => _config;

    /// <summary>
    /// 启动采集：每台启用的设备开一个后台 Task，各自独立轮询。
    /// 一台设备掉线不会影响其他设备，这是多设备采集的基本要求。
    /// </summary>
    public void Start()
    {
        if (IsRunning) return; // 已经在跑，别重复启动

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        var devices = _config.Devices.Where(d => d.Enabled).ToList();
        if (devices.Count == 0)
        {
            Log.Warn("配置里没有启用的设备，采集未启动。");
            _loops = Array.Empty<Task>();
            return;
        }

        // Task.Run 把每个 PollLoopAsync 丢到线程池，立即返回，不阻塞 UI
        _loops = devices.Select(d => Task.Run(() => PollLoopAsync(d, token), token)).ToArray();
        Log.Info($"采集启动，设备数 {devices.Count}。");
    }

    /// <summary>
    /// 停止采集：取消令牌 → 等所有循环退出 → 释放资源。
    /// 等待是必须的，否则窗口关了后台还在读串口，会报“端口被占用”。
    /// </summary>
    public async Task StopAsync()
    {
        var cts = _cts;
        if (cts is null) return;
        _cts = null;

        try { cts.Cancel(); } catch { }
        try { await Task.WhenAll(_loops).ConfigureAwait(false); } catch { /* 取消导致的异常忽略 */ }
        finally
        {
            cts.Dispose();
            _loops = Array.Empty<Task>();
            Log.Info("采集已停止。");
        }
    }

    /// <summary>
    /// 单台设备的采集循环：连接 → 分块读寄存器 → 解码 → 上报 → 按周期等待。
    /// 任何一步失败都会把整台设备标成 Bad 并上报，然后断开、等 2 秒重连。
    /// </summary>
    private async Task PollLoopAsync(DeviceConfig device, CancellationToken ct)
    {
        var groups = BuildGroups(device); // 启动时算一次，之后每轮复用
        using var master = _factory(device); // using 确保循环退出时释放连接/串口

        while (!ct.IsCancellationRequested)
        {
            var clock = Stopwatch.StartNew(); // 用来计算本轮实际耗时
            try
            {
                // 首次进入或掉线后，先建立连接
                if (!master.IsConnected)
                {
                    await master.ConnectAsync(ct).ConfigureAwait(false);
                    Log.Info($"[{device.Name}] 已连接 {device.Endpoint}");
                    DeviceStateChanged?.Invoke($"{device.Name} 已连接 {device.Endpoint}", true);
                }

                var values = new List<TagValue>(device.Points.Count);

                // 按“地址块”逐个读取；每块只发一帧，块内所有点共享这次数据
                foreach (var group in groups)
                {
                    var registers = await master.ReadHoldingRegistersAsync(
                        device.SlaveId, group.Start, (ushort)group.Count, ct).ConfigureAwait(false);

                    foreach (var point in group.Points)
                        values.Add(Decode(device, point, registers, group.Start));
                }

                RaiseSnapshot(values);
            }
            catch (OperationCanceledException)
            {
                // 用户点了停止/关闭窗口，正常退出，不当作故障
                break;
            }
            catch (Exception ex)
            {
                // 通信失败：记录日志、通知界面、把这台设备所有点标成 Bad
                Log.Error($"[{device.Name}] 采集失败：{ex.Message}", ex);
                DeviceStateChanged?.Invoke($"{device.Name} 通信异常：{ex.Message}", false);
                RaiseSnapshot(BuildBadSnapshot(device, ex.Message));

                // 断开后等 2 秒再重连，避免疯狂重试打爆设备
                try { master.Disconnect(); } catch { }
                if (!await DelayAsync(2000, ct).ConfigureAwait(false)) break;
                continue;
            }

            // 周期补偿：本轮已经花了 N 毫秒，就只再等“周期 - N”
            // 最少等 50ms，防止配置成 0 时把 CPU 和总线打满
            var wait = Math.Max(50, device.PollIntervalMs - (int)clock.ElapsedMilliseconds);
            if (!await DelayAsync(wait, ct).ConfigureAwait(false)) break;
        }

        Log.Info($"[{device.Name}] 采集循环退出。");
    }

    /// <summary>
    /// 把地址相邻的点位合并成块读取。
    /// 例：100、101、102、105 会合成 [100..105] 一次读 6 个寄存器，
    /// 中间空地址读回来丢弃即可——多读 2 个寄存器的代价远小于多发一帧。
    /// </summary>
    internal static List<RegisterGroup> BuildGroups(DeviceConfig device)
    {
        var result = new List<RegisterGroup>();

        // 按地址从小到大排序，才能把相邻地址合并到同一块
        foreach (var point in device.Points.Where(p => p.Enabled).OrderBy(p => p.Address))
        {
            var count = RegisterCodec.RegisterCount(point.DataType);
            var last = result.LastOrDefault();

            // 能合并的条件：
            // ① 和上一块起点不重叠；
            // ② 地址间隔不超过 2 个寄存器（间隔太大就不值得多读）；
            // ③ 合并后总长度不超过 125（Modbus 单帧上限）。
            var canMerge = last is not null
                           && point.Address <= last.Start + last.Count + 2
                           && point.Address >= last.Start
                           && point.Address + count - last.Start <= MaxRegistersPerRead;

            if (canMerge)
            {
                // 把点加进上一块，并扩大块的覆盖范围
                last!.Points.Add(point);
                last.Count = Math.Max(last.Count, point.Address + count - last.Start);
            }
            else
            {
                // 开一个新块
                var group = new RegisterGroup { Start = point.Address, Count = count };
                group.Points.Add(point);
                result.Add(group);
            }
        }
        return result;
    }

    /// <summary>
    /// 把“这一块寄存器”解码成工程值，并判断报警。
    /// groupStart 是块起始地址，用来算出该点数据在数组里的偏移。
    /// </summary>
    private static TagValue Decode(DeviceConfig device, PointConfig point, ushort[] registers, int groupStart)
    {
        var count = RegisterCodec.RegisterCount(point.DataType);

        // AsSpan(偏移, 长度)：只取这个点自己的那几个寄存器
        var raw = RegisterCodec.Decode(registers.AsSpan(point.Address - groupStart, count), point.DataType, point.Order);
        var engineering = RegisterCodec.ToEngineering(raw, point.Scale, point.Offset);

        // 报警判断：超上限报 High，低于下限报 Low，都在范围内就是 None
        var alarm = AlarmLevel.None;
        if (point.HighLimit is { } high && engineering > high) alarm = AlarmLevel.High;
        else if (point.LowLimit is { } low && engineering < low) alarm = AlarmLevel.Low;

        return new TagValue
        {
            DeviceName = device.Name,
            TagName = point.Name,
            Value = Math.Round(engineering, 3),
            Unit = point.Unit,
            Timestamp = DateTime.Now,
            Quality = Quality.Good,
            Alarm = alarm
        };
    }

    /// <summary>
    /// 通信失败时生成“坏快照”：每个点都给一条 Bad 记录。
    /// 这样界面不会停留在上一次的旧值上，操作工能立刻看出数据已失效。
    /// </summary>
    private static List<TagValue> BuildBadSnapshot(DeviceConfig device, string error) =>
        device.Points.Where(p => p.Enabled).Select(p => new TagValue
        {
            DeviceName = device.Name,
            TagName = p.Name,
            Value = 0,
            Unit = p.Unit,
            Timestamp = DateTime.Now,
            Quality = Quality.Bad,
            Alarm = AlarmLevel.None,
            Error = error
        }).ToList();

    /// <summary>触发快照事件。注意：事件在后台线程触发，UI 必须自己切回主线程。</summary>
    private void RaiseSnapshot(IReadOnlyList<TagValue> values)
    {
        try
        {
            SnapshotReady?.Invoke(values);
        }
        catch (Exception ex)
        {
            // 订阅者（UI）自己的异常不能影响采集线程。
            Log.Error($"处理采集快照时出错：{ex.Message}", ex);
        }
    }

    /// <summary>
    /// 等待指定毫秒。返回 false 表示等待期间被取消（用户停止采集），调用方应退出循环。
    /// </summary>
    private static async Task<bool> DelayAsync(int milliseconds, CancellationToken ct)
    {
        try
        {
            await Task.Delay(milliseconds, ct).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    /// <summary>异步释放：等价于 StopAsync，窗口关闭时用 await using 调用。</summary>
    public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);

    /// <summary>一次连续读取的寄存器块，以及块里包含的点位。</summary>
    internal sealed class RegisterGroup
    {
        /// <summary>块起始地址。</summary>
        public ushort Start { get; set; }

        /// <summary>块覆盖的寄存器数量。</summary>
        public int Count { get; set; }

        /// <summary>这块数据里包含的所有采集点。</summary>
        public List<PointConfig> Points { get; } = new();
    }
}
