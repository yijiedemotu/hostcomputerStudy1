using System.Text;

namespace ModbusMonitor;

/// <summary>
/// Modbus 采集监控程序入口。
/// 启动前先注册中文编码、挂全局异常处理，然后打开主窗体。
/// </summary>
internal static class Program
{
    /// <summary>[STAThread] 是 WinForms 界面线程的要求，不能删。</summary>
    [STAThread]
    private static void Main()
    {
        // 中文编码（GB2312）支持；工业仪表返回中文时要用。
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // 初始化 WinForms（高 DPI、默认字体等）
        ApplicationConfiguration.Initialize();

        // 界面线程异常：记日志 + 弹窗，避免程序“一闪就没”
        Application.ThreadException += (_, e) =>
        {
            Industrial.Core.Diagnostics.Log.Error("界面线程未处理异常", e.Exception);
            MessageBox.Show($"界面异常：\n{e.Exception.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        };

        // 其他线程的未处理异常：只能记日志，因为进程通常会被终止
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Industrial.Core.Diagnostics.Log.Error("进程级未处理异常", e.ExceptionObject as Exception);

        Application.Run(new MainForm());
    }
}
