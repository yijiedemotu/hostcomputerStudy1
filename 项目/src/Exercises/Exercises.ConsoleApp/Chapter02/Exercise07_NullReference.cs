using System.Runtime.CompilerServices;

namespace Exercises.ConsoleApp;

/// <summary>
/// 第 02 章 · 练习 7：故意制造空引用异常，再用断点找到出错位置。
///
/// 题目：写一段会抛 NullReferenceException 的代码，通过调试器定位到具体行。
///
/// 什么是空引用：
/// 引用类型（class）的变量可以指向一个对象，也可以是 null（什么都没有）。
/// 对着 null 访问属性或方法，运行时会抛 NullReferenceException。
/// 这是 C# 上位机现场最常见的异常之一，通常来自“配置没读到”“设备列表为空”“还没连接”。
///
/// 怎么用断点找：
/// 1. 在 TriggerNullReference 里 config!.Name 那一行左侧点一下，出现红点（或按 F9）；
/// 2. 按 F5 以调试方式启动；
/// 3. 程序停在该行时，看“局部变量”窗口，会看到 config 的值是 null；
/// 4. 按 F10 单步执行，异常就在这一行抛出。
/// 修复方式：先判空，或者用 ?. 安全访问。
/// </summary>
internal static class Exercise07_NullReference
{
    public static void Run()
    {
        ConsoleUi.Section("练习 7：故意写出空引用异常，用断点找到它");

        // try/catch 接住异常，程序不会崩溃，还能把异常信息打印出来供分析。
        try
        {
            TriggerNullReference();
        }
        catch (NullReferenceException ex)
        {
            // GetType().Name 得到异常类型名，这里就是 NullReferenceException。
            ConsoleUi.KeyValue("异常类型", ex.GetType().Name);
            ConsoleUi.KeyValue("异常信息", ex.Message);

            // StackTrace 是“调用栈”，记录异常发生时程序走过的路径。
            // 每行大致是“方法名 in 文件:行号”，第一行通常就是出错位置。
            var firstFrame = ex.StackTrace?
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault()?.Trim();
            ConsoleUi.KeyValue("出错位置", firstFrame ?? "（未包含 PDB 行号）");
        }

        ConsoleUi.Note("调试答案：在 TriggerNullReference 的 config!.Name 那一行按 F9 下断点，F5 启动，");
        ConsoleUi.Note("F10 单步到该行时看“局部变量”窗口，config 为 null；修复方式是判空或使用 ?.Name。");
    }

    /// <summary>
    /// 刻意让 config 保持 null，然后访问它的 Name 属性，制造空引用异常。
    /// NoInlining 告诉编译器“不要把这个小方法合并进调用方”，
    /// 这样异常堆栈里能清楚看到是本方法出错，方便练习调试。
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void TriggerNullReference()
    {
        // 类型后面的 ? 表示这个变量允许为 null。这里故意不创建对象，直接赋 null。
        PointConfig? config = null;

        // config! 里的感叹号是“空容忍运算符”：向编译器保证“我确定它不是 null”。
        // 但运行时它确实是 null，所以读取 .Name 会立刻抛 NullReferenceException。
        // 正确写法示例：var name = config?.Name ?? "(未配置)";
        Console.WriteLine($"    准备读取配置名称：{config!.Name}");
    }

    /// <summary>一个最简单的配置类，只用来演示空引用。</summary>
    private sealed class PointConfig
    {
        public string Name { get; set; } = "";
    }
}
