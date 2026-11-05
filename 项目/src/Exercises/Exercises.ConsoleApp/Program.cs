using System.Text;

namespace Exercises.ConsoleApp;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        ConsoleUi.Banner();

        if (args.Length > 0)
        {
            await RunAsync(args[0]);
            return;
        }

        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("  选择要查看的章节：");
            Console.WriteLine("    [2]  第 02 章  C# 核心语法（7 题）");
            Console.WriteLine("    [7]  第 07 章  Modbus 完全指南（6 题）");
            Console.WriteLine("    [10] 第 10 章  多线程与定时采集（5 题）");
            Console.WriteLine("    [A]  依次运行全部");
            Console.WriteLine("    [Q]  退出");
            Console.Write("  输入 > ");

            var choice = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(choice)) continue;
            if (choice.Equals("q", StringComparison.OrdinalIgnoreCase)) return;

            await RunAsync(choice);

            Console.WriteLine();
            Console.WriteLine("  按回车返回菜单（Ctrl+C 退出）...");
            Console.ReadLine();
        }
    }

    private static async Task RunAsync(string rawChoice)
    {
        var choice = rawChoice.Trim().ToLowerInvariant();
        try
        {
            switch (choice)
            {
                case "2" or "02":
                    await Chapter02.RunAsync();
                    break;
                case "7" or "07":
                    await Chapter07.RunAsync();
                    break;
                case "10":
                    await Chapter10.RunAsync();
                    break;
                case "a" or "all":
                    await Chapter02.RunAsync();
                    await Chapter07.RunAsync();
                    await Chapter10.RunAsync();
                    break;
                default:
                    ConsoleUi.Warn($"未知选项“{rawChoice}”，可用：2 / 7 / 10 / all。");
                    break;
            }
        }
        catch (Exception ex)
        {
            ConsoleUi.Error($"运行练习时出现未处理异常：{ex.GetType().Name}: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}
