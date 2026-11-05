namespace Exercises.WinForms;

/// <summary>
/// WinForms 练习程序的启动入口。
///
/// WinForms 程序的基本结构：
/// 1. [STAThread]：告诉 Windows“这个线程按单线程单元模式运行”。
///    WinForms 的剪贴板、文件对话框、拖放等 COM 功能依赖它，漏写会出现奇怪错误。
/// 2. ApplicationConfiguration.Initialize()：.NET 6+ 自动生成的方法，
///    统一设置高 DPI、默认字体等，相当于旧项目的 Application.EnableVisualStyles()。
/// 3. 挂两个异常处理事件：界面线程和后台线程漏掉的异常，弹窗提示而不是闪退。
/// 4. Application.Run(new MainForm())：启动消息循环。这行不返回，程序就一直运行，
///    直到主窗体关闭。
/// </summary>
internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        // 界面线程（UI 线程）里没被 try/catch 接住的异常，会走到这里。
        Application.ThreadException += (_, e) =>
            MessageBox.Show($"界面线程异常：\n{e.Exception.Message}", "错误",
                MessageBoxButtons.OK, MessageBoxIcon.Error);

        // 非界面线程的未处理异常（例如 Task 里抛出的）会走到这里。
        // 这里只做提示；生产程序还应写日志，方便现场排查。
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            MessageBox.Show($"未处理的异常：\n{e.ExceptionObject}", "严重错误",
                MessageBoxButtons.OK, MessageBoxIcon.Error);

        // 创建主窗体并进入消息循环：鼠标、键盘、重绘都靠它分发。
        Application.Run(new MainForm());
    }
}
