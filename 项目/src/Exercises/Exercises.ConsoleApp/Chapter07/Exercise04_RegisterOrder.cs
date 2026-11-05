using Industrial.Core.Modbus;

namespace Exercises.ConsoleApp;

/// <summary>
/// 第 07 章 · 练习 4：同一组寄存器按四种“字序”解释，结果完全不同。
///
/// 背景：
/// 一个 32 位浮点数（float）要占 2 个 16 位寄存器。
/// 设备厂家把这 2 个寄存器的高低字节排列成什么样，没有统一规定，于是出现四种常见字序：
///   ABCD = 0x1234, 0x5678（最常见）
///   BADC = 0x3412, 0x7856
///   CDAB = 0x5678, 0x1234
///   DCBA = 0x7856, 0x3412
///
/// 为什么上位机要学这个：
/// 现场“数值离谱”的第一嫌疑就是字序选错。比如 123.456 可能被解释成 1.5E+35。
/// 同一组字节用 UInt32 和 Float32 两种类型读，也会得到不同的数，因为解释规则不同。
/// </summary>
internal static class Exercise04_RegisterOrder
{
    public static void Run()
    {
        ConsoleUi.Section("练习 4：同一组寄存器在四种字序下的数值");

        // 原始两个寄存器。X4 表示按十六进制输出 4 位。
        ushort[] registers = [0x1234, 0x5678];
        ConsoleUi.KeyValue("原始寄存器", $"0x{registers[0]:X4}, 0x{registers[1]:X4}");

        // Enum.GetValues<RegisterOrder>() 会依次取出枚举里的四个值：Abcd、Badc、Cdab、Dcba。
        foreach (var order in Enum.GetValues<RegisterOrder>())
        {
            // 同样两个寄存器：
            // 按 UInt32 解释，得到的是整数；
            // 按 Float32 解释，得到的是浮点数。
            var u32 = RegisterCodec.Decode(registers, RegisterDataType.UInt32, order);
            var f32 = RegisterCodec.Decode(registers, RegisterDataType.Float32, order);

            // 某些字节组合不是合法浮点数（NaN 或无穷大），显示时要处理，避免打印成怪字符。
            var f32Text = double.IsFinite(f32) ? f32.ToString("G9") : "NaN/∞（无效浮点）";
            ConsoleUi.KeyValue(order.ToString(), $"UInt32 = {u32.ToString("N0").PadRight(14)} Float32 = {f32Text}");
        }

        ConsoleUi.Note("同一组字节按不同顺序解释，会得到完全不同的数；这就是现场“数值离谱”的第一嫌疑。");

        // 反过来做一次：把 123.456 按 ABCD 编码成 2 个寄存器，
        // 再用四种字序解码，只有 ABCD 能还原。
        var encoded = RegisterCodec.Encode(123.456, RegisterDataType.Float32, RegisterOrder.Abcd);
        ConsoleUi.KeyValue("把 123.456 按 ABCD 编码", $"0x{encoded[0]:X4}, 0x{encoded[1]:X4}");
        foreach (var order in Enum.GetValues<RegisterOrder>())
        {
            var value = RegisterCodec.Decode(encoded, RegisterDataType.Float32, order);
            ConsoleUi.KeyValue($"用 {order} 解码", double.IsFinite(value) ? value.ToString("G9") : "NaN/∞");
        }
        ConsoleUi.Answer("只有 ABCD 解码回 123.456；其他三种字序会得到无意义的值。现场必须按设备手册选对字序。");
    }
}
