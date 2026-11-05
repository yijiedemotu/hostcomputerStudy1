namespace Answers.ConsoleApp;

/// <summary>
/// 第 02 章的总入口：按顺序调用 7 个独立的练习文件。
///
/// 菜单里输入 2（或启动参数传 2）时，Program 会执行这里的 RunAsync。
/// 每道题的代码分别在 Chapter02 文件夹下的 Exercise01 ~ Exercise07 文件中。
/// 想单独研究某道题，直接打开对应文件即可，不必在长文件里找。
/// </summary>
internal static class Chapter02
{
    public static async Task RunAsync()
    {
        ConsoleUi.Chapter("第 02 章 · C# 核心语法（7 题）");

        // 前 5 题是同步方法，直接调用。
        Exercise01_ParseHex.Run();
        Exercise02_ToHex.Run();
        Exercise03_Linq.Run();
        Exercise04_Json.Run();
        Exercise05_Event.Run();

        // 第 6 题是异步方法，返回 Task，必须 await 等它执行完，
        // 否则第 7 题会先执行，输出顺序就乱了。
        await Exercise06_Async.RunAsync();

        // 第 7 题会故意抛异常，但方法内部已经用 try/catch 接住，不会让程序退出。
        Exercise07_NullReference.Run();
    }
}
