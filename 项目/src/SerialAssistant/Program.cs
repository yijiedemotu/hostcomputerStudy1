using System.Text;

namespace SerialAssistant;

/// <summary>
/// 程序入口。WinForms 程序的启动流程：
/// 注册编码 → 初始化 WinForms 配置 → 挂全局异常处理 → 打开主窗体。
/// </summary>
internal static class Program
{
    /// <summary>
    /// [STAThread] 是 WinForms 的硬性要求：界面线程必须使用“单线程单元”模型，
    /// 否则剪贴板、文件对话框、COM 组件都会出问题。删掉它程序可能直接报错。
    /// </summary>
    [STAThread]
    private static void Main()
    {
        // 注册代码页，才能用 GB2312/GBK 解码国产仪表返回的中文。
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // .NET 6+ 的 WinForms 初始化：设置高 DPI、默认字体等，替代老的 Application.EnableVisualStyles()
        ApplicationConfiguration.Initialize();

        // 全局异常兜底：上位机不能“一闪就没了”，至少要把异常告诉用户并记日志。
        // ThreadException 捕获界面线程（按钮点击、定时器）里没处理的异常。
        Application.ThreadException += (_, e) =>
            MessageBox.Show($"界面线程异常：\n{e.Exception.Message}", "错误",
                MessageBoxButtons.OK, MessageBoxIcon.Error);

        // UnhandledException 捕获其他后台线程里没处理的异常。
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            MessageBox.Show($"未处理的异常：\n{e.ExceptionObject}", "严重错误",
                MessageBoxButtons.OK, MessageBoxIcon.Error);

        // 启动消息循环并显示主窗体；这行之后的代码要等窗口关闭才执行。
        Application.Run(new MainForm());
    }
}
