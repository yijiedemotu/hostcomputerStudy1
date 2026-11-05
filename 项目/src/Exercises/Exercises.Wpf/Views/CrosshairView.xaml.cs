using System.Windows.Controls;
using System.Windows.Threading;
using Exercises.Wpf.ViewModels;

namespace Exercises.Wpf.Views;

/// <summary>
/// 第 05 章练习 2 的 View（界面后台代码）。
///
/// 页面只做一件事：每 500ms 调一次 ViewModel.Pump()，模拟采集。
/// 数据、暂停状态、清空逻辑都在 CrosshairViewModel 里；
/// 自绘图表通过绑定监听 Points 集合变化并自动重绘。
///
/// DispatcherTimer 必须由 UI 线程创建和触发，这样修改 ObservableCollection
/// 才不会触发“另一个线程拥有此集合”的 WPF 异常。
/// </summary>
public partial class CrosshairView : UserControl
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(500) };

    public CrosshairView()
    {
        InitializeComponent();

        // DataContext 可能尚未设置好，所以用 as 安全转换，null 时跳过本次 Tick。
        _timer.Tick += (_, _) => (DataContext as CrosshairViewModel)?.Pump();

        // 页面显示时启动，离开时停止，避免后台空转。
        Loaded += (_, _) => _timer.Start();
        Unloaded += (_, _) => _timer.Stop();
    }
}
