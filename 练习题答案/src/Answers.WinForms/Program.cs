namespace Answers.WinForms;

/// <summary>
/// WinForms 答案程序的启动入口。
///
/// [STAThread] 是 WinForms 的硬性要求：剪贴板、文件对话框、拖放等
/// 都依赖“单线程单元（STA）”，漏写会在运行时出现奇怪错误。
///
/// ApplicationConfiguration.Initialize() 由 .NET 6+ 自动生成，
/// 用来统一设置高 DPI 和默认字体，等价于旧项目的 EnableVisualStyles。
/// </summary>
internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        // 界面线程未处理的异常：弹窗提示，避免直接闪退。
        Application.ThreadException += (_, e) =>
            MessageBox.Show($"界面线程异常：\n{e.Exception.Message}", "错误",
                MessageBoxButtons.OK, MessageBoxIcon.Error);

        // 后台线程未处理的异常同样给出提示。生产程序还应写日志。
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            MessageBox.Show($"未处理的异常：\n{e.ExceptionObject}", "严重错误",
                MessageBoxButtons.OK, MessageBoxIcon.Error);

        Application.Run(new MainForm());
    }
}
