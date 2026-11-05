using System.Windows;
using Exercises.Wpf.ViewModels;

namespace Exercises.Wpf;

/// <summary>
/// 主窗口的后台代码。
///
/// 在 MVVM 里，后台代码只做“纯界面”的事：
/// - 设置 DataContext（把 ViewModel 交给界面绑定）；
/// - 处理窗口拖动、最小化/最大化/关闭这类窗口行为。
/// 业务逻辑（报警、趋势数据、筛选）都在 ViewModels 文件夹里。
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();   // 加载 MainWindow.xaml 里定义的控件和资源

        // DataContext 是 WPF 绑定的“默认数据源”。
        // 设置后，XAML 里的 {Binding Sections} 会自动去 MainViewModel 找 Sections 属性。
        DataContext = new MainViewModel();

        // 窗口最大化/还原时，更新右上角按钮的图标。
        StateChanged += (_, _) => UpdateMaximizeGlyph();
    }

    // 下面三个是自定义标题栏按钮的点击处理。
    // 因为窗口用了 WindowChrome，隐藏了系统标题栏，必须自己实现最小化/最大化/关闭。
    private void Minimize_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void Maximize_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    /// <summary>根据当前窗口状态切换最大化/还原图标。</summary>
    private void UpdateMaximizeGlyph() =>
        MaxButton.Content = WindowState == WindowState.Maximized ? "▣" : "□";
}
