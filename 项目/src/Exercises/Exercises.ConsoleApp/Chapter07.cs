namespace Exercises.ConsoleApp;

/// <summary>
/// 第 07 章的总入口：按顺序调用 6 个独立的练习文件。
///
/// 菜单里输入 7（或启动参数传 7）时，Program 会执行这里的 RunAsync。
/// 每道题的完整代码分别在 Chapter07 文件夹下的 Exercise01 ~ Exercise06 文件中。
/// </summary>
internal static class Chapter07
{
    public static async Task RunAsync()
    {
        ConsoleUi.Chapter("第 07 章 · Modbus 完全指南（6 题）");

        // 练习 1、5、6 内部有异步操作，需要 await。
        await Exercise01_SendFrame.RunAsync();

        // 练习 2、3、4 是纯计算，同步执行即可。
        Exercise02_ParseResponse.Run();
        Exercise03_Crc.Run();
        Exercise04_RegisterOrder.Run();

        await Exercise05_Polling100ms.RunAsync();
        await Exercise06_Reconnect.RunAsync();
    }
}
