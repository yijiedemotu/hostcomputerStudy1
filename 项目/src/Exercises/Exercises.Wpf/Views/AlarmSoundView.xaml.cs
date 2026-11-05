using System.Windows.Controls;
using System.Windows.Threading;
using Exercises.Wpf.ViewModels;

namespace Exercises.Wpf.Views;

/// <summary>
/// 第 05 章练习 1 的 View（界面后台代码）。
///
/// 为什么定时器放在这里而不是 ViewModel：
/// DispatcherTimer 绑定 WPF 的 UI 线程调度器，属于界面层。
/// ViewModel 只提供 Tick() 和“有没有活动报警/是否静音”这些数据。
///
/// 声音策略：
/// - 每秒调用一次 VM.Tick() 更新温度；
/// - 有活动报警且未静音时，最多每 4 秒响一次，避免持续刺耳；
/// - 静音只影响声音，报警记录仍然正常写入。
/// </summary>
public partial class AlarmSoundView : UserControl
{
    // Interval = 1 秒。DispatcherTimer 的 Tick 在 UI 线程执行，可以直接改绑定数据。
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };

    private DateTime _lastSoundAt = DateTime.MinValue;

    public AlarmSoundView()
    {
        InitializeComponent();
        _timer.Tick += OnTick;

        // Loaded：控件真正显示到界面时启动定时器；
        // Unloaded：切换页面/关闭窗口时停掉，避免隐藏页面继续跑。
        Loaded += (_, _) => _timer.Start();
        Unloaded += (_, _) => _timer.Stop();
    }

    /// <summary>每秒执行：驱动 VM 更新数据，再决定要不要响铃。</summary>
    private void OnTick(object? sender, EventArgs e)
    {
        // DataContext 是上层 DataTemplate 传进来的 AlarmSoundViewModel。
        if (DataContext is not AlarmSoundViewModel viewModel) return;

        viewModel.Tick();

        // 没有活动报警，或用户已静音：只更新数据，不播放声音。
        if (!viewModel.HasActiveAlarms || viewModel.IsMuted) return;

        // 限频：距离上次响铃不足 4 秒就跳过。
        if ((DateTime.Now - _lastSoundAt).TotalSeconds < 4) return;

        _lastSoundAt = DateTime.Now;

        // SystemSounds 是 Windows 自带的提示音，不需要音频文件。
        // 生产程序通常用 MediaPlayer 播放自定义报警声，并支持音量/循环。
        System.Media.SystemSounds.Exclamation.Play();
    }
}
