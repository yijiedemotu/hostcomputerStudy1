namespace Exercises.ConsoleApp;

/// <summary>
/// 第 02 章 · 练习 1：把十六进制字符串解析成 byte[]。
///
/// 题目：ParseHex("01 03 00 00 00 0A") 返回 6 个字节；遇到非法字符抛 FormatException。
///
/// 为什么上位机要学这个：
/// 串口和网口传的都是二进制。调试工具为了让人看懂，会显示成 "01 03 00 00 ..." 这样的文本。
/// 程序要先把这种“给人看的文本”还原成字节数组，后面才能计算 CRC、发送报文。
///
/// 阅读顺序：
/// 1. Run() 是这道题的入口；
/// 2. 先演示正确输入，再用 try/catch 演示两种错误输入；
/// 3. 真正的解析算法在 HexUtil.cs 的 ParseHex 方法里。
/// </summary>
internal static class Exercise01_ParseHex
{
    public static void Run()
    {
        ConsoleUi.Section("练习 1：ParseHex(\"01 03 00 00 00 0A\") → byte[]，非法字符抛 FormatException");

        // var 表示“由编译器自动推断类型”。右边返回 byte[]，所以 data 就是 byte[]。
        // byte 是 0~255 的整数，一个 byte 正好是一个字节，这是通信数据最常用的类型。
        var data = HexUtil.ParseHex("01 03 00 00 00 0A");

        // Length 是数组长度：题目给了 6 个字节，所以这里应当是 6。
        ConsoleUi.KeyValue("返回长度", data.Length);

        // 这一行用了三样东西：
        // 1. Select：LINQ 的“转换”，把每个 byte 变成 "0x01" 这样的字符串；
        // 2. $"0x{b:X2}"：字符串插值，X2 表示按十六进制输出且至少两位；
        // 3. string.Join：用逗号把转换后的字符串拼成一行。
        ConsoleUi.KeyValue("字节内容", string.Join(", ", data.Select(b => $"0x{b:X2}")));
        ConsoleUi.Ok("合法输入解析正确。");

        // try/catch 是“异常处理”：
        // 可能出错的语句放在 try 里；一旦抛出异常，就跳到对应的 catch 继续执行。
        // 这里传入 GG，G 不是十六进制字符（只允许 0-9、A-F），解析器应当主动报错。
        try
        {
            HexUtil.ParseHex("01 03 GG");
        }
        catch (FormatException ex)
        {
            // ex 是异常对象，Message 里是给程序员看的错误说明。
            ConsoleUi.KeyValue("非法字符测试", ex.Message);
        }

        // 一个字节要用两个十六进制字符表示，所以字符个数必须是偶数。
        // "010" 只有 3 个字符，无法两两配对，也必须报错。
        try
        {
            HexUtil.ParseHex("010");
        }
        catch (FormatException ex)
        {
            ConsoleUi.KeyValue("奇数长度测试", ex.Message);
        }

        // 小结：工具方法不“猜”调用方的意图。输入不合法就直接抛 FormatException，
        // 调用方必须显式处理，错误就不会悄悄流进后面的通信流程。
    }
}
