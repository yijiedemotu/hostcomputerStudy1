using System.Windows;
using System.Windows.Threading;

namespace Exercises.Wpf;

/// <summary>
/// WPF 程序的启动类（App.xaml 的后台代码）。
///
/// App.xaml 里的 StartupUri="MainWindow.xaml" 指定了启动窗口；
/// WPF 会自动调用 InitializeComponent 加载 XAML 资源，然后创建并显示主窗口。
///
/// 这里额外挂了一个界面线程异常处理：
/// WPF 的 UI 线程如果抛出未处理异常，默认会直接结束进程（用户看到程序闪退）。
/// 接住并提示，至少能让用户知道发生了什么；生产程序还应写日志。
/// </summary>
public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show($"界面线程异常：\n{e.Exception.Message}", "错误",
            MessageBoxButton.OK, MessageBoxImage.Error);

        // Handled = true 表示“这个异常我已经处理了”，阻止 WPF 结束进程。
        // 注意：不是所有异常都能安全恢复，生产程序应记录日志并评估是否继续运行。
        e.Handled = true;
    }
}
