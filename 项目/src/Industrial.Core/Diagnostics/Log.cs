using System.Text;

namespace Industrial.Core.Diagnostics;

/// <summary>
/// 极简文件日志：logs/yyyyMMdd.log，按天滚动。
///
/// 上位机必须记日志的三个理由：
///  1. 现场出问题，操作工只会说“它自己就断了”，你得靠日志还原；
///  2. 通信异常要区分“偶发一次”和“每分钟 10 次”，后者要报修；
///  3. 客户追责时日志就是证据。
///
/// 生产项目建议 Serilog/NLog，但原理和这里一致：异步写、按天滚动、保留 N 天。
/// </summary>
public static class Log
{
    // 多线程同时写日志时，用锁保证一行一行完整落盘
    private static readonly object Sync = new();

    // 默认日志目录：程序 exe 所在目录下的 logs
    private static string _directory = Path.Combine(AppContext.BaseDirectory, "logs");

    /// <summary>UI 可以订阅它实现“日志窗口”。注意回调在后台线程，要 Invoke 回 UI 线程。</summary>
    public static event Action<string>? MessageWritten;

    /// <summary>初始化日志目录，并清理 30 天前的旧日志。程序启动时调用一次。</summary>
    public static void Init(string directory)
    {
        _directory = directory;
        Directory.CreateDirectory(_directory);
        CleanupOldFiles(keepDays: 30);
    }

    // 三个快捷方法，对应常见的日志级别
    public static void Info(string message) => Write("INFO", message, null);
    public static void Warn(string message) => Write("WARN", message, null);
    public static void Error(string message, Exception? ex = null) => Write("ERROR", message, ex);

    /// <summary>
    /// 写一行日志：时间 + 级别 + 内容，有异常时再附上异常类型和消息。
    /// 日志本身出错（磁盘满、没权限）只吞掉，绝不能让主程序崩溃。
    /// </summary>
    public static void Write(string level, string message, Exception? ex)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
        if (ex is not null)
            line += Environment.NewLine + "    " + ex.GetType().Name + ": " + ex.Message;

        lock (Sync)
        {
            try
            {
                Directory.CreateDirectory(_directory);
                // 按天分文件：logs/20261023.log
                var path = Path.Combine(_directory, $"{DateTime.Now:yyyyMMdd}.log");
                File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
            }
            catch
            {
                // 日志失败绝不能把主流程带崩（磁盘满、无权限都可能发生）。
            }
        }

        // 通知界面（如果有订阅），例如主窗口的日志列表
        MessageWritten?.Invoke(line);
    }

    /// <summary>删除超过 keepDays 天的日志文件，避免日志无限增长。</summary>
    private static void CleanupOldFiles(int keepDays)
    {
        try
        {
            var cutoff = DateTime.Now.Date.AddDays(-keepDays);
            foreach (var file in Directory.EnumerateFiles(_directory, "*.log"))
            {
                if (File.GetLastWriteTime(file) < cutoff)
                {
                    try { File.Delete(file); } catch { }
                }
            }
        }
        catch { }
    }
}
