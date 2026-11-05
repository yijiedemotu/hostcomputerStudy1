using System.Windows;
using System.Windows.Threading;

namespace ScadaDashboard;

/// <summary>
/// WPF 应用程序入口。StartupUri 在 App.xaml 里指定了 MainWindow，
/// 所以这里只需要挂全局异常处理。
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // WPF 的全局异常兜底：Dispatcher 未处理异常不接住，程序直接消失。
        // DispatcherUnhandledException 捕获 UI 线程异常，可以标记 Handled 继续运行。
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        // 非 UI 线程的异常只能记日志，进程通常会被终止
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            Industrial.Core.Diagnostics.Log.Error("进程级异常", args.ExceptionObject as Exception);
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Industrial.Core.Diagnostics.Log.Error("界面线程未处理异常", e.Exception);

        // WPF 用 MessageBox（不是 WinForms 的 MessageBox）
        MessageBox.Show($"程序出现异常：\n\n{e.Exception.Message}", "错误",
            MessageBoxButton.OK, MessageBoxImage.Error);

        // 标记已处理，程序继续运行（工业现场最忌讳界面直接崩掉）。
        e.Handled = true;
    }
}
