namespace Answers.ConsoleApp.Modbus;

/// <summary>
/// Modbus RTU 的 CRC16：初值 0xFFFF、多项式 0xA001（反向表示）、结果不异或。
/// 发送时低字节在前，这是现场最常见的出错点。
/// </summary>
public static class Crc16
{
    public static ushort Compute(ReadOnlySpan<byte> data)
    {
        ushort crc = 0xFFFF;

        foreach (var b in data)
        {
            // 先把当前字节异或到 CRC 的低 8 位。
            crc ^= b;

            // 再对 8 个二进制位逐位处理。
            for (var i = 0; i < 8; i++)
            {
                if ((crc & 0x0001) != 0)
                    crc = (ushort)((crc >> 1) ^ 0xA001);
                else
                    crc >>= 1;
            }
        }

        return crc;
    }

    /// <summary>校验整帧：重新计算除末尾两字节以外的内容，再与帧尾比较。</summary>
    public static bool Check(ReadOnlySpan<byte> frame)
    {
        if (frame.Length < 4) return false;

        var expected = Compute(frame[..^2]);
        return frame[^2] == (byte)(expected & 0xFF)
               && frame[^1] == (byte)(expected >> 8);
    }

    /// <summary>在原数据后追加 CRC，返回可以直接发送的新数组。</summary>
    public static byte[] Append(ReadOnlySpan<byte> data)
    {
        var crc = Compute(data);
        var result = new byte[data.Length + 2];
        data.CopyTo(result);

        // 低字节在前。
        result[^2] = (byte)(crc & 0xFF);
        result[^1] = (byte)(crc >> 8);
        return result;
    }
}

/// <summary>寄存器里保存的数据类型。16 位占 1 个寄存器，32 位占 2 个。</summary>
public enum RegisterDataType
{
    UInt16,
    Int16,
    UInt32,
    Int32,
    Float32,
    Float64
}

/// <summary>32 位数据的字序。现场“数值离谱”时，先核对这个。</summary>
public enum RegisterOrder
{
    Abcd,
    Badc,
    Cdab,
    Dcba
}

/// <summary>寄存器数组与数值之间的转换。</summary>
public static class RegisterCodec
{
    public static int RegisterCount(RegisterDataType type) => type switch
    {
        RegisterDataType.UInt16 or RegisterDataType.Int16 => 1,
        RegisterDataType.UInt32 or RegisterDataType.Int32 or RegisterDataType.Float32 => 2,
        RegisterDataType.Float64 => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    /// <summary>把寄存器解析成数值：先摊平成字节，再按字序重排，最后按类型拼接。</summary>
    public static double Decode(ReadOnlySpan<ushort> registers, RegisterDataType type,
        RegisterOrder order = RegisterOrder.Abcd)
    {
        var count = RegisterCount(type);
        if (registers.Length < count)
            throw new ArgumentException($"需要 {count} 个寄存器，实际只有 {registers.Length} 个", nameof(registers));

        // Modbus 规定单个寄存器内部高字节在前，先摊平成字节数组。
        Span<byte> raw = stackalloc byte[count * 2];
        for (var i = 0; i < count; i++)
        {
            raw[i * 2] = (byte)(registers[i] >> 8);
            raw[i * 2 + 1] = (byte)(registers[i] & 0xFF);
        }

        Span<byte> canonical = stackalloc byte[count * 2];
        Reorder(raw, canonical, order);

        return type switch
        {
            RegisterDataType.UInt16 => (ushort)((canonical[0] << 8) | canonical[1]),
            RegisterDataType.Int16 => (short)((canonical[0] << 8) | canonical[1]),
            RegisterDataType.UInt32 => ((uint)canonical[0] << 24) | ((uint)canonical[1] << 16) |
                                       ((uint)canonical[2] << 8) | canonical[3],
            RegisterDataType.Int32 => (int)(((uint)canonical[0] << 24) | ((uint)canonical[1] << 16) |
                                           ((uint)canonical[2] << 8) | canonical[3]),
            RegisterDataType.Float32 => BitConverter.Int32BitsToSingle(
                (canonical[0] << 24) | (canonical[1] << 16) | (canonical[2] << 8) | canonical[3]),
            RegisterDataType.Float64 => BitConverter.Int64BitsToDouble(
                ((long)canonical[0] << 56) | ((long)canonical[1] << 48) | ((long)canonical[2] << 40) |
                ((long)canonical[3] << 32) | ((long)canonical[4] << 24) | ((long)canonical[5] << 16) |
                ((long)canonical[6] << 8) | canonical[7]),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }

    /// <summary>把数值编码成寄存器数组。</summary>
    public static ushort[] Encode(double value, RegisterDataType type,
        RegisterOrder order = RegisterOrder.Abcd)
    {
        var count = RegisterCount(type);
        Span<byte> canonical = stackalloc byte[count * 2];

        switch (type)
        {
            case RegisterDataType.UInt16:
            {
                var v = (ushort)Math.Round(value);
                canonical[0] = (byte)(v >> 8);
                canonical[1] = (byte)v;
                break;
            }
            case RegisterDataType.Int16:
            {
                var v = (short)Math.Round(value);
                canonical[0] = (byte)((ushort)v >> 8);
                canonical[1] = (byte)v;
                break;
            }
            case RegisterDataType.UInt32:
            case RegisterDataType.Int32:
            {
                var v = type == RegisterDataType.UInt32
                    ? (uint)Math.Round(value)
                    : unchecked((uint)(int)Math.Round(value));
                canonical[0] = (byte)(v >> 24);
                canonical[1] = (byte)(v >> 16);
                canonical[2] = (byte)(v >> 8);
                canonical[3] = (byte)v;
                break;
            }
            case RegisterDataType.Float32:
            {
                var bits = BitConverter.SingleToInt32Bits((float)value);
                canonical[0] = (byte)(bits >> 24);
                canonical[1] = (byte)(bits >> 16);
                canonical[2] = (byte)(bits >> 8);
                canonical[3] = (byte)bits;
                break;
            }
            case RegisterDataType.Float64:
            {
                var bits = BitConverter.DoubleToInt64Bits(value);
                for (var i = 0; i < 8; i++)
                    canonical[i] = (byte)(bits >> (56 - i * 8));
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(type));
        }

        Span<byte> raw = stackalloc byte[count * 2];
        Reorder(canonical, raw, order);

        var result = new ushort[count];
        for (var i = 0; i < count; i++)
            result[i] = (ushort)((raw[i * 2] << 8) | raw[i * 2 + 1]);
        return result;
    }

    /// <summary>map 表示“目标第 i 个字节取自源的第几个字节”。四种置换都是自逆的。</summary>
    private static void Reorder(ReadOnlySpan<byte> source, Span<byte> target, RegisterOrder order)
    {
        var map = order switch
        {
            RegisterOrder.Abcd => new[] { 0, 1, 2, 3 },
            RegisterOrder.Badc => new[] { 1, 0, 3, 2 },
            RegisterOrder.Cdab => new[] { 2, 3, 0, 1 },
            RegisterOrder.Dcba => new[] { 3, 2, 1, 0 },
            _ => throw new ArgumentOutOfRangeException(nameof(order))
        };

        for (var i = 0; i < source.Length; i++)
            target[i] = source[map[i % 4] + i / 4 * 4];
    }
}

/// <summary>
/// Modbus 主站统一接口。真实项目的 TCP/RTU 两种实现都遵循它，
/// 采集代码只认接口，换协议时不用改上层逻辑。
/// </summary>
public interface IModbusMaster : IDisposable
{
    bool IsConnected { get; }
    string Endpoint { get; }

    Task ConnectAsync(CancellationToken ct = default);
    void Disconnect();

    Task<bool[]> ReadCoilsAsync(byte unitId, ushort startAddress, ushort count, CancellationToken ct = default);
    Task<bool[]> ReadDiscreteInputsAsync(byte unitId, ushort startAddress, ushort count, CancellationToken ct = default);
    Task<ushort[]> ReadHoldingRegistersAsync(byte unitId, ushort startAddress, ushort count, CancellationToken ct = default);
    Task<ushort[]> ReadInputRegistersAsync(byte unitId, ushort startAddress, ushort count, CancellationToken ct = default);
    Task WriteSingleCoilAsync(byte unitId, ushort address, bool value, CancellationToken ct = default);
    Task WriteSingleRegisterAsync(byte unitId, ushort address, ushort value, CancellationToken ct = default);
    Task WriteMultipleRegistersAsync(byte unitId, ushort startAddress, ushort[] values, CancellationToken ct = default);
}
