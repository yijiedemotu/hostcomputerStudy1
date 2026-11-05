namespace Answers.ConsoleApp;

internal static class ConsoleUi
{
    public static void Banner()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║          上位机学习大全 · 课后练习参考答案                   ║");
        Console.WriteLine("║          第 02 / 07 / 10 章（控制台部分）                     ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.ResetColor();
    }

    public static void Chapter(string title)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(new string('═', 66));
        Console.WriteLine(title);
        Console.WriteLine(new string('═', 66));
        Console.ResetColor();
    }

    public static void Section(string title)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"▶ {title}");
        Console.ResetColor();
    }

    public static void Answer(string text)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  答案：{text}");
        Console.ResetColor();
    }

    public static void Note(string text)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"  说明：{text}");
        Console.ResetColor();
    }

    public static void Ok(string text)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  ✔ {text}");
        Console.ResetColor();
    }

    public static void Warn(string text)
    {
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine($"  ! {text}");
        Console.ResetColor();
    }

    public static void Error(string text)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  ✘ {text}");
        Console.ResetColor();
    }

    public static void KeyValue(string key, object? value)
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write($"  {key,-18}");
        Console.ResetColor();
        Console.WriteLine(value);
    }
}
