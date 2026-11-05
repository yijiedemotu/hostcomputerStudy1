using System.Windows;
using System.Windows.Threading;
using Answers.Wpf.ViewModels;

namespace Answers.Wpf.Windows;

/// <summary>
/// 第 05 章练习 2 的窗口后台。
///
/// 窗口只做一件事：每 500ms 调一次 ViewModel.Pump()，模拟采集。
/// 数据、暂停状态、清空逻辑都在 CrosshairViewModel 里；
/// 自绘图表通过依赖属性监听 Points 集合变化并自动重绘。
///
/// DispatcherTimer 由 UI 线程创建和触发，
/// 因此修改 ObservableCollection 不会触发“另一个线程拥有此集合”的 WPF 异常。
/// </summary>
public partial class CrosshairWindow : Window
{
    private readonly CrosshairViewModel _viewModel = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(500) };

    public CrosshairWindow()
    {
        InitializeComponent();

        DataContext = _viewModel;

        _timer.Tick += (_, _) => _viewModel.Pump();

        Loaded += (_, _) => _timer.Start();
        Closed += (_, _) => _timer.Stop();
    }
}
