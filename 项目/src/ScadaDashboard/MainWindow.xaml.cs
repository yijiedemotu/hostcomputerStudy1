using System.ComponentModel;
using System.Windows;
using ScadaDashboard.ViewModels;

namespace ScadaDashboard;

/// <summary>
/// 视图层只做三件事：搭界面、把 ViewModel 挂上去、把“画图”这类纯 UI 动作接上。
/// 业务逻辑一行都不写在这里——这就是 MVVM 的边界。
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent(); // 按 XAML 创建界面

        _viewModel = new MainViewModel();

        // 把 ViewModel 挂到窗口上，XAML 里的 {Binding ...} 才能找到数据
        DataContext = _viewModel;

        // 选中位号变化 → 清空趋势图并换标题/单位/报警限
        _viewModel.SelectedTagChanged += tag =>
            Trend.Reset(tag?.Name ?? "未选择位号", tag?.Unit ?? "", tag?.LowLimit, tag?.HighLimit);

        // 每次采到新值 → 只有当前选中的位号才追加到趋势图
        _viewModel.PointAdded += (tag, value, time) =>
        {
            if (ReferenceEquals(tag, _viewModel.SelectedTag))
                Trend.AddPoint(time, value);
        };

        // 窗口加载后默认选中第一张卡片
        Loaded += (_, _) =>
        {
            if (_viewModel.Tags.Count > 0)
                _viewModel.SelectedTag = _viewModel.Tags[0];
        };
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // 关窗口前停掉采集线程，否则进程会残留后台任务。
        try { _viewModel.DisposeAsync().AsTask().Wait(1500); } catch { }
        base.OnClosing(e);
    }
}
