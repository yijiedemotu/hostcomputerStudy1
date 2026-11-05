using System.Collections.ObjectModel;
using System.Windows.Input;
using Exercises.Wpf.Models;

namespace Exercises.Wpf.ViewModels;

/// <summary>
/// 第 05 章练习 2：趋势十字光标的 ViewModel。
///
/// 分工：
/// - 本类维护 Points（数据点集合）和顶部当前值文字；
/// - View 里的 DispatcherTimer 每 500ms 调用一次 Pump()，模拟采集；
/// - 自绘控件 CrosshairTrendChart 绑定 Points，鼠标移动时显示十字线和提示。
///
/// 为什么定时器不放在 ViewModel：
/// DispatcherTimer 属于 WPF 的界面调度器。VM 保持“纯数据”，
/// 换成 WinForms/控制台/单元测试时更容易复用。
/// </summary>
public sealed class CrosshairViewModel : ObservableObject
{
    private readonly Random _random = new();
    private double _phase;                 // 正弦波相位，用于生成平滑曲线
    private bool _isRunning = true;
    private string _currentText = "--.-";
    private string _timeText = "等待采集";

    public CrosshairViewModel()
    {
        ToggleCommand = new RelayCommand(ToggleRunning);
        ClearCommand = new RelayCommand(Clear);

        // 先造 45 个历史点，时间从 22.5 秒前开始，每 0.5 秒一个，打开就能看到曲线。
        var start = DateTime.Now.AddSeconds(-22.5);
        for (var i = 0; i < 45; i++)
        {
            _phase += 0.28;
            var value = NextValue();
            Points.Add(new TrendPoint(start.AddMilliseconds(i * 500), value));
        }

        var last = Points[^1];
        CurrentText = last.Value.ToString("F2");
        TimeText = last.Time.ToString("HH:mm:ss.fff");
    }

    /// <summary>趋势数据。ObservableCollection 增删项会通知图表重绘。</summary>
    public ObservableCollection<TrendPoint> Points { get; } = new();

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (!SetProperty(ref _isRunning, value)) return;
            OnPropertyChanged(nameof(ToggleText));
            OnPropertyChanged(nameof(RunStateText));
        }
    }

    public string ToggleText => IsRunning ? "暂停采集" : "继续采集";
    public string RunStateText => IsRunning ? "采集中" : "已暂停";

    public string CurrentText
    {
        get => _currentText;
        private set => SetProperty(ref _currentText, value);
    }

    public string TimeText
    {
        get => _timeText;
        private set => SetProperty(ref _timeText, value);
    }

    public string SampleCountText => $"{Points.Count} / 120 点";

    public ICommand ToggleCommand { get; }
    public ICommand ClearCommand { get; }

    /// <summary>由 View 每 500ms 调用一次，模拟一次采集。</summary>
    public void Pump()
    {
        if (!IsRunning) return;

        _phase += 0.28;
        var point = new TrendPoint(DateTime.Now, NextValue());
        Points.Add(point);

        // 只保留最近 120 个点，形成滚动窗口。
        if (Points.Count > 120)
            Points.RemoveAt(0);

        CurrentText = point.Value.ToString("F2");
        TimeText = point.Time.ToString("HH:mm:ss.fff");

        // Points.Count 变了，但集合通知只负责“项变化”，计算属性要手动通知。
        OnPropertyChanged(nameof(SampleCountText));
    }

    /// <summary>正弦波 + 小噪声，生成看起来像真实温度的值。</summary>
    private double NextValue()
    {
        var value = 25 + 3.2 * Math.Sin(_phase / 5) + (_random.NextDouble() - 0.5) * 0.7;
        return Math.Round(value, 2);
    }

    private void ToggleRunning() => IsRunning = !IsRunning;

    private void Clear()
    {
        Points.Clear();
        CurrentText = "--.-";
        TimeText = "等待采集";
        OnPropertyChanged(nameof(SampleCountText));
    }
}
