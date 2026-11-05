using Industrial.Core.Modbus;

namespace Exercises.ConsoleApp;

/// <summary>
/// 第 07 章 · 练习 3：手算一帧写单寄存器请求的 CRC。
///
/// 题目：对 01 06 00 05 03 78 计算 CRC16，结果应为 99 19。
///
/// 含义：把 1 号从站的 5 号寄存器写成 0x0378（十进制 888）。
///
/// CRC 参数（Modbus 标准）：
/// - 初值 0xFFFF
/// - 多项式 0xA001（反向表示）
/// - 结果不做异或
/// - 发送时低字节在前
///
/// 现场最常见的错误就是“高低字节发反了”：数值明明算对，从站却一直不回应。
/// </summary>
internal static class Exercise03_Crc
{
    public static void Run()
    {
        ConsoleUi.Section("练习 3：手算 01 06 00 05 03 78 的 CRC，和 99 19 对比");

        // 只取帧头到数据末尾，不含 CRC。CRC 就是针对这部分计算的。
        var payload = HexUtil.ParseHex("01 06 00 05 03 78");

        // Crc16.Compute 返回 ushort，例如 0x1999。
        var crc = Crc16.Compute(payload);

        // 拆成低字节和高字节：
        // & 0xFF 取出最低 8 位；>> 8 把高 8 位移到低 8 位。
        var low = (byte)(crc & 0xFF);
        var high = (byte)(crc >> 8);

        ConsoleUi.KeyValue("计算结果", $"0x{crc:X4}");
        ConsoleUi.KeyValue("线上发送顺序（低字节在前）", $"{low:X2} {high:X2}");
        ConsoleUi.KeyValue("与 99 19 对比", crc == 0x1999 ? "一致 ✔" : "不一致 ✘");

        // Append 会复制原数据并追加正确的 CRC，返回完整可发送的帧。
        ConsoleUi.KeyValue("完整 RTU 帧", HexUtil.ToHex(Crc16.Append(payload)));
        ConsoleUi.Note("CRC 初值 0xFFFF、多项式 0xA001、结果异或 0x0000；注意低字节先发，这是现场最常见的错误。");
    }
}
