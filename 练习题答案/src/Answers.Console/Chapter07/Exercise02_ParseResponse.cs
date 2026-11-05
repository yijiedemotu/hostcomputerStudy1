using Answers.ConsoleApp.Modbus;

namespace Answers.ConsoleApp;

/// <summary>
/// 第 07 章 · 练习 2：解析一帧读寄存器应答。
///
/// 题目：解析 01 03 08 00 64 00 C8 01 2C 01 90 90 08，说出 4 个寄存器的值。
///
/// 注意：教程早期示例把 CRC 写成了 BA 7A，这是笔误。
/// 按数据区重新计算，正确的 CRC 是 90 08（低字节 90 在前），本文件按正确帧演示。
///
/// 帧结构：
///   01       从站地址
///   03       功能码
///   08       后面数据区的字节数 = 4 个寄存器 × 2
///   00 64    第 0 个寄存器 = 100
///   00 C8    第 1 个寄存器 = 200
///   01 2C    第 2 个寄存器 = 300
///   01 90    第 3 个寄存器 = 400
///   90 08    CRC
/// </summary>
internal static class Exercise02_ParseResponse
{
    public static void Run()
    {
        ConsoleUi.Section("练习 2：解析 01 03 08 00 64 00 C8 01 2C 01 90 90 08");

        var response = HexUtil.ParseHex("01 03 08 00 64 00 C8 01 2C 01 90 90 08");

        // 计算 CRC 时不能把帧尾自带的两个 CRC 字节算进去，
        // 所以 AsSpan(0, 长度 - 2) 只截取“从站地址到数据区末尾”。
        var computedCrc = Crc16.Compute(response.AsSpan(0, response.Length - 2));

        // ^2 表示“倒数第 2 个元素”，^1 是最后一个。
        // Crc16 返回 ushort，高位是 CRC 高字节；线上先发低字节，所以要拆开看。
        ConsoleUi.KeyValue("帧内 CRC（低字节在前）", $"{response[^2]:X2} {response[^1]:X2}");
        ConsoleUi.KeyValue("重新计算 CRC", $"{computedCrc & 0xFF:X2} {computedCrc >> 8:X2}");
        ConsoleUi.KeyValue("CRC 校验", Crc16.Check(response) ? "正确 ✔" : "错误 ✘");

        // 固定位置的含义：第 0 字节从站地址、第 1 字节功能码、第 2 字节数据字节数。
        ConsoleUi.KeyValue("从站地址 / 功能码 / 字节数", $"{response[0]} / 0x{response[1]:X2} / {response[2]}");

        // 数据从第 3 个字节开始，每 2 个字节一个寄存器。
        // 移位和按位或：高字节左移 8 位，再和低字节拼起来。
        // 例如 0x01 和 0x2C → 0x012C → 300。
        var values = new ushort[4];
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = (ushort)((response[3 + i * 2] << 8) | response[4 + i * 2]);
            ConsoleUi.KeyValue($"寄存器 {i}", $"0x{values[i]:X4} = {values[i]}");
        }

        ConsoleUi.Answer($"四个值分别是 {string.Join("、", values)}（0x0064、0x00C8、0x012C、0x0190）。");
        ConsoleUi.Note("寄存器按大端传输：高字节在前。若设备手册给的是 40001 基址，协议地址要减 1（40001 → 0）。");
    }
}
