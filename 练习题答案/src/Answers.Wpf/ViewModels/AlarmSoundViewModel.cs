using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Answers.Wpf.ViewModels;

/// <summary>
/// 第 05 章练习 1：报警声音、静音与确认复位。
///
/// 分层设计（本练习最重要的一点）：
/// - ViewModel（本文件）只负责“数据”和“规则”：温度变化、活动报警数、静音状态、确认复位；
/// - View（AlarmSoundView.xaml.cs）负责“界面副作用”：用 DispatcherTimer 每秒调用 Tick，
///   并在有活动报警且未静音时播放系统提示音。
/// 这样 ViewModel 不引用 Dispatcher、不引用控件，换成控制台或 Web 也能复用。
///
/// 报警规则：
/// - 温度随机游走，超过 85℃ 连续 3 次才产生报警，避免瞬时抖动误报；
/// - 两次报警至少间隔 6 秒，避免刷屏；
/// - 活动报警最多累计 9 条；
/// - “确认”会把活动报警清零，并把温度压回阈值以下，模拟操作员处理后恢复。
/// </summary>
public sealed class AlarmSoundViewModel : ObservableObject
{
    private readonly Random _random = new();

    private double _temperature = 74.2;       // 当前温度（℃）
    private int _activeAlarms;                // 未确认的活动报警数
    private int _ackCount;                    // 操作员确认次数
    private bool _isMuted;                    // 是否静音
    private int _highTicks;                   // 连续超限的 Tick 次数
    private DateTime _lastAlarmAt = DateTime.MinValue;   // 上一次报警时间，用于限频

    public AlarmSoundViewModel()
    {
        // RelayCommand 把方法包装成命令，XAML 的 Button.Command 可以直接绑定。
        MuteCommand = new RelayCommand(() => IsMuted = !IsMuted);
        AcknowledgeCommand = new RelayCommand(Acknowledge);
        TriggerAlarmCommand = new RelayCommand(TriggerManualAlarm);

        RecentAlarms.Add($"{DateTime.Now:HH:mm:ss}  采集启动，当前温度 {Temperature:F1} ℃");
    }

    public string DeviceName => "1# 加热炉";

    /// <summary>报警阈值：85℃。</summary>
    public double Threshold => 85.0;

    /// <summary>当前温度。private set 表示只有 ViewModel 自己能改，界面不能直接写。</summary>
    public double Temperature
    {
        get => _temperature;
        private set => SetProperty(ref _temperature, value);
    }

    /// <summary>
    /// 活动报警数。它的变化会连带影响两个计算属性：
    /// HasActiveAlarms 和 AlarmStatusText，所以要手动补发通知。
    /// </summary>
    public int ActiveAlarms
    {
        get => _activeAlarms;
        private set
        {
            if (!SetProperty(ref _activeAlarms, value)) return;
            OnPropertyChanged(nameof(HasActiveAlarms));
            OnPropertyChanged(nameof(AlarmStatusText));
        }
    }

    public int AcknowledgeCount
    {
        get => _ackCount;
        private set => SetProperty(ref _ackCount, value);
    }

    public bool HasActiveAlarms => _activeAlarms > 0;

    public string AlarmStatusText => HasActiveAlarms
        ? $"{ActiveAlarms} 条活动报警，等待确认"
        : "当前无活动报警";

    /// <summary>
    /// 静音开关。变化时通知 MuteButtonText 和 SoundStateText，
    /// 否则按钮文字和状态说明不会刷新。
    /// </summary>
    public bool IsMuted
    {
        get => _isMuted;
        set
        {
            if (!SetProperty(ref _isMuted, value)) return;
            OnPropertyChanged(nameof(MuteButtonText));
            OnPropertyChanged(nameof(SoundStateText));
        }
    }

    public string MuteButtonText => IsMuted ? "取消静音" : "静音报警声";
    public string SoundStateText => IsMuted ? "静音中 · 只记录不发声" : "声音开启 · 报警时每 4 秒提示一次";

    /// <summary>最近报警记录，最新的插在最前面；ObservableCollection 会自动刷新 ListBox。</summary>
    public ObservableCollection<string> RecentAlarms { get; } = new();

    public ICommand MuteCommand { get; }
    public ICommand AcknowledgeCommand { get; }
    public ICommand TriggerAlarmCommand { get; }

    /// <summary>
    /// 由 View 的 DispatcherTimer 每秒调用一次，模拟一次采集。
    /// VM 不知道是谁调用的，也不关心界面，只更新数据。
    /// </summary>
    public void Tick()
    {
        // 随机游走：每次在 -0.72 ~ +0.78 之间漂移，偶尔（1/45 概率）额外 +7，制造一次明显升温。
        var drift = (_random.NextDouble() - 0.48) * 1.5;
        if (_random.Next(0, 45) == 0) drift += 7;

        // Math.Clamp 把温度限制在 60~96，避免跑飞。
        Temperature = Math.Clamp(Temperature + drift, 60, 96);

        if (Temperature >= Threshold)
        {
            _highTicks++;

            // 连续 3 秒超限，并且距离上次报警已满 6 秒，才真正报警。
            if (_highTicks >= 3 && (DateTime.Now - _lastAlarmAt).TotalSeconds >= 6)
            {
                _highTicks = 0;
                RaiseAlarm($"温度高高限 {Temperature:F1} ℃（阈值 {Threshold:F1} ℃）");
            }
        }
        else
        {
            // 一旦回到阈值以下，连续超限计数清零，避免“累计三次”误报。
            _highTicks = 0;
        }
    }

    /// <summary>按钮“手动触发报警”：把温度抬到至少 88.6，再走正常报警流程。</summary>
    private void TriggerManualAlarm()
    {
        Temperature = Math.Max(Temperature, 88.6);
        RaiseAlarm("手动模拟：温度高高限");
    }

    /// <summary>产生一条报警：更新活动数、插入历史记录。</summary>
    private void RaiseAlarm(string message)
    {
        _lastAlarmAt = DateTime.Now;

        // 活动报警最多显示 9 条，避免数字无限增长。
        ActiveAlarms = Math.Min(ActiveAlarms + 1, 9);

        // Insert(0, ...) 让最新记录排在最上面。
        RecentAlarms.Insert(0, $"{DateTime.Now:HH:mm:ss}  {message}");
        TrimHistory();
    }

    /// <summary>操作员确认：清空活动报警，并把温度降到 79℃ 以下，模拟处理完成。</summary>
    private void Acknowledge()
    {
        AcknowledgeCount++;

        if (ActiveAlarms == 0)
        {
            RecentAlarms.Insert(0, $"{DateTime.Now:HH:mm:ss}  操作员巡检确认（当前无活动报警）");
        }
        else
        {
            ActiveAlarms = 0;
            Temperature = Math.Min(Temperature, 79.0);
            _highTicks = 0;
            RecentAlarms.Insert(0, $"{DateTime.Now:HH:mm:ss}  操作员确认并复位，活动报警清零");
        }

        TrimHistory();
    }

    /// <summary>历史记录只保留最近 30 条，防止长时间运行后列表无限变长。</summary>
    private void TrimHistory()
    {
        while (RecentAlarms.Count > 30)
            RecentAlarms.RemoveAt(RecentAlarms.Count - 1);
    }
}
