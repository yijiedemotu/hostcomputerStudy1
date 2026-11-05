using System.Windows;
using System.Windows.Threading;

namespace Answers.Wpf;

/// <summary>
/// WPF 程序的启动类（App.xaml 的后台代码）。
///
/// StartupUri="MainWindow.xaml" 指定启动窗口；
/// WPF 会自动加载 App.xaml 里的主题与样式资源，再创建主窗口。
///
/// 这里额外接住 UI 线程未处理异常：默认行为是直接结束进程（用户看到闪退），
/// 弹窗提示至少能让人知道发生了什么。生产程序还应写日志。
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

        // Handled = true 表示异常已处理，阻止 WPF 结束进程。
        // 注意：并非所有异常都能安全恢复，生产程序应记录日志并评估是否继续运行。
        e.Handled = true;
    }
}
