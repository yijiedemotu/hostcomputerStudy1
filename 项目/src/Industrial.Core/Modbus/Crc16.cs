namespace Industrial.Core.Modbus;

/// <summary>
/// Modbus RTU 的 CRC16 校验（多项式 0xA001，初值 0xFFFF），低字节先发。
/// 一句话原理：把每个字节依次异或进 16 位寄存器，再做 8 次“右移 + 条件异或”，
/// 最后得到两个字节的校验码。面试常要求手写，建议背下来。
/// </summary>
public static class Crc16
{
    /// <summary>计算一段数据的 CRC16，返回 16 位整数。</summary>
    public static ushort Compute(ReadOnlySpan<byte> data)
    {
        // 初值固定为 0xFFFF，这是 Modbus 规范要求的
        ushort crc = 0xFFFF;

        foreach (var b in data)
        {
            // 1. 当前字节与 CRC 低 8 位异或
            crc ^= b;

            // 2. 对 8 个二进制位逐位处理
            for (var i = 0; i < 8; i++)
            {
                if ((crc & 0x0001) != 0)
                    // 最低位是 1：先右移一位，再异或多项式 0xA001
                    crc = (ushort)((crc >> 1) ^ 0xA001);
                else
                    // 最低位是 0：只右移
                    crc >>= 1;
            }
        }
        return crc;
    }

    /// <summary>
    /// 校验整帧：Modbus RTU 一帧的最后两个字节就是 CRC，且“低字节在前”。
    /// 做法是把前面所有内容重新算一遍 CRC，再和末尾两字节比较。
    /// </summary>
    public static bool Check(ReadOnlySpan<byte> frame)
    {
        // 最短也要有 从站号 + 功能码 + CRC 两字节，少于 4 字节肯定不合法
        if (frame.Length < 4) return false;

        // frame[..^2] 表示“除最后两个字节以外的全部内容”
        var expected = Compute(frame[..^2]);
        return frame[^2] == (byte)(expected & 0xFF) && frame[^1] == (byte)(expected >> 8);
    }

    /// <summary>在一段数据后追加 CRC，返回新数组（发送帧前常用）。</summary>
    public static byte[] Append(ReadOnlySpan<byte> data)
    {
        var crc = Compute(data);

        // 新数组比原数据多 2 个字节，用来放 CRC
        var result = new byte[data.Length + 2];
        data.CopyTo(result);

        // 注意顺序：低字节在前，高字节在后
        result[^2] = (byte)(crc & 0xFF);
        result[^1] = (byte)(crc >> 8);
        return result;
    }
}
