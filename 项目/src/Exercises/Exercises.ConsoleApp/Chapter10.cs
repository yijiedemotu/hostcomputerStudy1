namespace Exercises.ConsoleApp;

/// <summary>
/// 第 10 章的总入口：按顺序调用 5 个独立的练习文件。
///
/// 菜单里输入 10（或启动参数传 10）时，Program 会执行这里的 RunAsync。
/// 每道题的完整代码分别在 Chapter10 文件夹下的 Exercise01 ~ Exercise05 文件中。
/// </summary>
internal static class Chapter10
{
    public static async Task RunAsync()
    {
        ConsoleUi.Chapter("第 10 章 · 多线程、定时采集与线程安全（5 题）");

        // 第 1、2、3、5 题都有异步等待，必须 await，保证按顺序完成。
        await Exercise01_TimerComparison.RunAsync();
        await Exercise02_ChannelDecoupling.RunAsync();
        await Exercise03_Deadlock.RunAsync();

        // 第 4 题只做内存里的状态判定，同步执行。
        Exercise04_FailureThreshold.Run();

        await Exercise05_FlushOnClose.RunAsync();
    }
}
