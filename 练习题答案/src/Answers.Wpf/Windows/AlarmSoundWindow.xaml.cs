using System.Windows;
using System.Windows.Threading;
using Answers.Wpf.ViewModels;

namespace Answers.Wpf.Windows;

/// <summary>
/// 第 05 章练习 1 的窗口后台。
///
/// 分工是这道题的核心：
/// - ViewModel 只提供 Tick()、HasActiveAlarms、IsMuted 等数据和规则；
/// - 窗口用 DispatcherTimer 每秒驱动一次 Tick()；
/// - 只有“有活动报警且未静音”时，窗口才播放系统提示音，并限制每 4 秒最多响一次。
///
/// 这样 ViewModel 不引用 Dispatcher、不引用控件，
/// 换到控制台或单元测试里也能直接复用。
/// </summary>
public partial class AlarmSoundWindow : Window
{
    private readonly AlarmSoundViewModel _viewModel = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };

    private DateTime _lastSoundAt = DateTime.MinValue;

    public AlarmSoundWindow()
    {
        InitializeComponent();

        DataContext = _viewModel;

        _timer.Tick += OnTick;

        // 窗口显示时启动，关闭时停止，避免关闭后定时器还在后台空转。
        Loaded += (_, _) => _timer.Start();
        Closed += (_, _) => _timer.Stop();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        _viewModel.Tick();

        if (!_viewModel.HasActiveAlarms || _viewModel.IsMuted) return;
        if ((DateTime.Now - _lastSoundAt).TotalSeconds < 4) return;

        _lastSoundAt = DateTime.Now;

        // SystemSounds 是 Windows 自带提示音，不需要音频文件。
        // 生产程序通常用 MediaPlayer 播放自定义报警声，并支持音量/循环。
        System.Media.SystemSounds.Exclamation.Play();
    }
}
