using System.Collections.ObjectModel;
using Industrial.Core.Diagnostics;
using Industrial.Core.Models;
using Industrial.Core.Modbus;
using Industrial.Core.Services;

namespace ScadaDashboard.ViewModels;

/// <summary>
/// 主 ViewModel：所有界面状态的唯一来源。
///
/// 关键设计：
///  1. 采集在后台线程，界面更新通过构造时捕获的 SynchronizationContext 回到 UI 线程，
///     ViewModel 里不出现 Dispatcher，也不用 Control.Invoke；
///  2. 按钮用 ICommand，界面 XAML 里没有一行事件处理代码；
///  3. 报警只在状态翻转时入表（上升沿），避免每个采集周期刷一条。
///
/// 可以把它理解成 WinForms 窗体里的“字段 + 事件处理”全部搬到这里，
/// 但这里不继承任何控件，所以业务逻辑和界面彻底分开。
/// </summary>
public sealed class MainViewModel : ObservableObject, IAsyncDisposable
{
    // 捕获创建 ViewModel 时所在的同步上下文。
    // WPF 里就是 UI 线程的上下文，之后后台线程用它 Post，就能回到 UI 线程改属性。
    private readonly SynchronizationContext _uiContext;

    // 采集配置：设备、位号、地址、量程、上下限都从这里来。
    private readonly AppConfig _config;

    // 记录每个位号“上一次”的报警状态，用来判断报警是刚触发还是刚恢复。
    // 键是 TagViewModel.Key（设备名|位号）。
    private readonly Dictionary<string, AlarmLevel> _alarmState = new();

    private PollingService? _service;          // 采集服务；null 表示当前没在采集
    private TagViewModel? _selectedTag;        // 卡片区当前选中的位号（趋势图跟随它）
    private string _statusText = "未启动";
    private string _sampleCountText = "0 个快照";
    private string _lastUpdateText = "--";
    private bool _isRunning;

    /// <summary>
    /// 构造 ViewModel：准备配置、位号卡片和按钮命令。
    /// 必须先在 UI 线程创建它，否则 _uiContext 捕获不到界面上下文。
    /// </summary>
    public MainViewModel()
    {
        // SynchronizationContext.Current 在 UI 线程上不是 null；
        // 万一在非 UI 线程构造，就退化成普通上下文，至少不会直接崩溃。
        _uiContext = SynchronizationContext.Current ?? new SynchronizationContext();
        _config = CreateDemoConfig();

        // 把配置里的每个设备、每个位号，变成一个可绑定的 TagViewModel。
        // 只加载 Enabled = true 的设备。
        foreach (var device in _config.Devices.Where(d => d.Enabled))
        {
            foreach (var point in device.Points)
            {
                Tags.Add(new TagViewModel(device.Name, point.Name, point.Unit, point.LowLimit, point.HighLimit));
            }
        }

        // 三个按钮命令：
        //  - Start 只在“没运行”时可点；
        //  - Stop 只在“运行中”时可点；
        //  - 清空报警随时可点。
        StartCommand = new RelayCommand(Start, () => !IsRunning);
        StopCommand = new RelayCommand(Stop, () => IsRunning);
        ClearAlarmsCommand = new RelayCommand(() => Alarms.Clear());
    }

    /// <summary>所有位号卡片的数据源（ObservableCollection 增删会自动通知界面）。</summary>
    public ObservableCollection<TagViewModel> Tags { get; } = new();

    /// <summary>报警列表的数据源，最新的报警插在最前面。</summary>
    public ObservableCollection<AlarmItem> Alarms { get; } = new();

    // 按钮绑定的命令对象，XAML 里写 Command="{Binding StartCommand}"。
    public RelayCommand StartCommand { get; }
    public RelayCommand StopCommand { get; }
    public RelayCommand ClearAlarmsCommand { get; }

    /// <summary>
    /// 当前选中的位号变化（趋势图据此切换）。
    /// View 在窗口构造函数里订阅它，View 自己决定怎么画，ViewModel 不碰控件。
    /// </summary>
    public event Action<TagViewModel?>? SelectedTagChanged;

    /// <summary>
    /// 采到一个新点（趋势图据此追加点）。
    /// 只有“当前选中 + 数据质量良好”的位号才会触发。
    /// </summary>
    public event Action<TagViewModel, double, DateTime>? PointAdded;

    /// <summary>
    /// 选中的位号。setter 里用 Set 触发属性通知，
    /// 如果值真的变了，再通知趋势图切换曲线。
    /// </summary>
    public TagViewModel? SelectedTag
    {
        get => _selectedTag;
        set
        {
            if (!Set(ref _selectedTag, value)) return;   // 值没变就什么都不做
            SelectedTagChanged?.Invoke(value);
        }
    }

    /// <summary>状态栏文字，例如“采集运行中 · 127.0.0.1:502”。</summary>
    public string StatusText
    {
        get => _statusText;
        private set => Set(ref _statusText, value);     // 只允许 ViewModel 自己改
    }

    /// <summary>已收到多少个采集快照，用于确认采集是否真的在跑。</summary>
    public string SampleCountText
    {
        get => _sampleCountText;
        private set => Set(ref _sampleCountText, value);
    }

    /// <summary>最近一次界面刷新的时间。</summary>
    public string LastUpdateText
    {
        get => _lastUpdateText;
        private set => Set(ref _lastUpdateText, value);
    }

    /// <summary>
    /// 是否正在采集。改变时额外调用两个命令的 RaiseCanExecuteChanged，
    /// 这样“开始/停止”按钮会自动变灰或恢复可点。
    /// </summary>
    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (!Set(ref _isRunning, value)) return;
            StartCommand.RaiseCanExecuteChanged();
            StopCommand.RaiseCanExecuteChanged();
        }
    }

    /// <summary>
    /// “开始采集”按钮。防重入：如果服务已经存在就直接返回。
    /// </summary>
    private void Start()
    {
        if (_service is not null) return;

        // 创建采集服务并订阅两个事件，然后启动后台采集循环。
        _service = new PollingService(_config);
        _service.SnapshotReady += OnSnapshotReady;
        _service.DeviceStateChanged += OnDeviceStateChanged;
        _service.Start();

        IsRunning = true;
        StatusText = "采集运行中 · 127.0.0.1:502";
    }

    /// <summary>
    /// “停止采集”按钮。
    /// async void 只适合事件/命令入口：内部 await 停止服务，异常由全局处理器兜底。
    /// </summary>
    private async void Stop()
    {
        var service = _service;
        if (service is null) return;

        // 先把字段置空，防止重复点击“停止”时再次进入。
        _service = null;

        // 先退订事件，再停止服务，避免停止过程中还有回调进来。
        service.SnapshotReady -= OnSnapshotReady;
        service.DeviceStateChanged -= OnDeviceStateChanged;

        await service.StopAsync();   // 等待后台采集循环真正退出
        IsRunning = false;
        StatusText = "已停止";
        Log.Info("WPF 看板：采集已停止。");
    }

    /// <summary>
    /// 采集服务在后台线程触发这个事件。
    /// 注意：这里不能直接改 Tags，因为 WPF 控件只能在 UI 线程更新。
    /// </summary>
    private void OnSnapshotReady(IReadOnlyList<TagValue> values)
    {
        // 后台线程 → UI 线程。Post 是异步的，不会阻塞采集循环。
        // 真正更新界面的工作交给 ApplySnapshot，在 UI 线程执行。
        _uiContext.Post(_ => ApplySnapshot(values), null);
    }

    /// <summary>
    /// 设备连接状态变化（上线/掉线）也在后台线程触发，
    /// 同样要 Post 回 UI 线程再改属性。
    /// </summary>
    private void OnDeviceStateChanged(string message, bool online)
    {
        _uiContext.Post(_ =>
        {
            StatusText = message;
            Log.Info($"[设备状态] {message}（在线={online}）");
        }, null);
    }

    /// <summary>
    /// 在 UI 线程应用一整批采集结果：
    /// 更新每个位号、给趋势图加点、判断报警状态是否翻转。
    /// </summary>
    private void ApplySnapshot(IReadOnlyList<TagValue> values)
    {
        foreach (var value in values)
        {
            // 用“设备名 + 位号”找到对应的卡片。找不到就跳过（配置可能变了）。
            var tag = Tags.FirstOrDefault(t => t.Device == value.DeviceName && t.Name == value.TagName);
            if (tag is null) continue;

            // 更新卡片上显示的值、质量、时间。
            tag.Update(value);

            // 只有“质量良好”且“正是当前选中位号”时，才给趋势图追加一个点。
            // 坏值不画，否则曲线会突然掉到 0，看起来像设备故障。
            if (value.Quality == Quality.Good && ReferenceEquals(tag, SelectedTag))
                PointAdded?.Invoke(tag, value.Value, value.Timestamp);

            // ---- 报警边沿检测 ----
            // previous 是上一次的报警状态；value.Alarm 是这一次的。
            var previous = _alarmState.TryGetValue(tag.Key, out var level) ? level : AlarmLevel.None;

            // 状态没变（一直正常，或一直报警）就不重复记录。
            // 这样一次持续报警只会产生一条“触发”，不会每 500ms 刷屏。
            if (previous == value.Alarm) continue;

            _alarmState[tag.Key] = value.Alarm;

            if (value.Alarm != AlarmLevel.None)
            {
                // 正常 → 报警：记一条“触发”
                var levelText = value.Alarm == AlarmLevel.High ? "超上限" : "低于下限";
                Alarms.Insert(0, new AlarmItem(value.Timestamp, $"{value.DeviceName}/{value.TagName}", value.Value, levelText, "触发"));
                Log.Warn($"报警：{value.DeviceName}/{value.TagName} = {value.Value:0.###}{value.Unit}（{levelText}）");
            }
            else
            {
                // 报警 → 正常：记一条“恢复”，说明上次报的是什么。
                var previousText = previous == AlarmLevel.High ? "超上限" : "低于下限";
                Alarms.Insert(0, new AlarmItem(value.Timestamp, $"{value.DeviceName}/{value.TagName}", value.Value, previousText, "恢复"));
            }

            // 报警列表最多留 200 条，防止长时间运行内存一直涨。
            while (Alarms.Count > 200) Alarms.RemoveAt(Alarms.Count - 1);
        }

        // 每处理完一批快照，更新右上角的统计信息。
        // ++_sampleCount 先自增再使用，所以第一次显示 1。
        SampleCountText = $"{++_sampleCount} 个快照 · {Tags.Count} 个位号";
        LastUpdateText = DateTime.Now.ToString("HH:mm:ss.fff");
    }

    // 已收到的快照数量；放在这里是因为只被 ApplySnapshot 使用。
    private int _sampleCount;

    /// <summary>
    /// 演示配置。真实项目从 JSON 读，和 WinForms 项目共用同一个 AppConfig。
    ///
    /// 这里写死两个设备、六个位号，地址从 0 开始，配合教程里的 Modbus 模拟器使用。
    /// 每个 PointConfig 的 Scale 表示“寄存器原始值 × Scale = 工程值”。
    /// </summary>
    private static AppConfig CreateDemoConfig() => new()
    {
        Title = "产线监控看板",
        Devices =
        {
            new DeviceConfig
            {
                Name = "1#注塑机",
                Protocol = DeviceProtocol.ModbusTcp,
                Host = "127.0.0.1",
                Port = 502,
                SlaveId = 1,              // 站号 1
                PollIntervalMs = 500,     // 每 500ms 采集一轮
                Points =
                {
                    // 地址 0：模温，Int16，原始值 ×0.1 = ℃，40~220 为正常
                    new PointConfig { Name = "模温", Address = 0, DataType = RegisterDataType.Int16, Scale = 0.1, Unit = "℃", LowLimit = 40, HighLimit = 220 },
                    // 地址 1：注射压力，UInt16，原始值 ×0.01 = MPa，上限 120
                    new PointConfig { Name = "注射压力", Address = 1, DataType = RegisterDataType.UInt16, Scale = 0.01, Unit = "MPa", HighLimit = 120 },
                    // 地址 2：周期时间，UInt16，原始值 ×0.1 = 秒
                    new PointConfig { Name = "周期时间", Address = 2, DataType = RegisterDataType.UInt16, Scale = 0.1, Unit = "s" },
                    // 地址 3：累计产量，UInt32 占两个寄存器，不缩放
                    new PointConfig { Name = "累计产量", Address = 3, DataType = RegisterDataType.UInt32, Unit = "件" }
                }
            },
            new DeviceConfig
            {
                Name = "2#空压机",
                Protocol = DeviceProtocol.ModbusTcp,
                Host = "127.0.0.1",
                Port = 502,
                SlaveId = 2,              // 站号 2
                PollIntervalMs = 500,
                Points =
                {
                    // 地址 0：排气压力，UInt16，原始值 ×0.01 = MPa，上限 12
                    new PointConfig { Name = "排气压力", Address = 0, DataType = RegisterDataType.UInt16, Scale = 0.01, Unit = "MPa", HighLimit = 12 },
                    // 地址 1：排气温度，Int16，原始值 ×0.1 = ℃，上限 95
                    new PointConfig { Name = "排气温度", Address = 1, DataType = RegisterDataType.Int16, Scale = 0.1, Unit = "℃", HighLimit = 95 }
                }
            }
        }
    };

    /// <summary>
    /// 窗口关闭时调用（App 里通过 await vm.DisposeAsync() 触发）。
    /// 和 Stop 一样：先退订事件，再异步停止采集，防止后台线程还在跑。
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_service is not null)
        {
            var service = _service;
            _service = null;
            service.SnapshotReady -= OnSnapshotReady;
            service.DeviceStateChanged -= OnDeviceStateChanged;
            await service.StopAsync();
        }
    }
}
