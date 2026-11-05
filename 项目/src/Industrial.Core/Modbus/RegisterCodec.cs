namespace Industrial.Core.Modbus;

/// <summary>
/// 寄存器里装的数据类型。
/// 16 位的占 1 个寄存器，32 位的占 2 个，64 位的占 4 个。
/// 同一串寄存器，按不同类型解释会得到完全不同的值，必须和 PLC 手册一致。
/// </summary>
public enum RegisterDataType
{
    /// <summary>无符号 16 位：0 ~ 65535，常用于状态字、计数。</summary>
    UInt16,

    /// <summary>有符号 16 位：-32768 ~ 32767，温度、压力常用。</summary>
    Int16,

    /// <summary>无符号 32 位：0 ~ 42 亿，累计量常用。</summary>
    UInt32,

    /// <summary>有符号 32 位：-21 亿 ~ 21 亿。</summary>
    Int32,

    /// <summary>32 位单精度浮点，占 2 个寄存器，很多仪表用这个。</summary>
    Float32,

    /// <summary>64 位双精度浮点，占 4 个寄存器，精度要求高的场合用。</summary>
    Float64
}

/// <summary>
/// 32/64 位数据的字序（工业现场最坑的一个点）。
/// 以 0x12345678 为例，两个寄存器 [0x1234][0x5678]：
///   ABCD = 高字在前、字内大端（最标准，西门子/施耐德常见）
///   CDAB = 低字在前（部分电表、台达）
///   BADC = 字内字节交换（部分欧姆龙/三菱）
///   DCBA = 完全小端（汇川部分伺服、Modbus 转 Profibus 网关）
/// 拼错的表现是“数值离谱但量级差不多”，现场一定先核对字序。
/// </summary>
public enum RegisterOrder
{
    Abcd,
    Badc,
    Cdab,
    Dcba
}

/// <summary>寄存器与工程值互转。</summary>
public static class RegisterCodec
{
    /// <summary>某种数据类型要占几个寄存器。</summary>
    public static int RegisterCount(RegisterDataType type) => type switch
    {
        RegisterDataType.UInt16 or RegisterDataType.Int16 => 1,
        RegisterDataType.UInt32 or RegisterDataType.Int32 or RegisterDataType.Float32 => 2,
        RegisterDataType.Float64 => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    /// <summary>
    /// 把寄存器数组解析为原始数值（未做量程换算）。
    /// 步骤：① 每个寄存器拆成高/低两个字节 → ② 按字序重排成标准 ABCD →
    /// ③ 按数据类型把字节拼回数字。
    /// </summary>
    public static double Decode(ReadOnlySpan<ushort> registers, RegisterDataType type, RegisterOrder order = RegisterOrder.Abcd)
    {
        var count = RegisterCount(type);
        if (registers.Length < count)
            throw new ArgumentException($"需要 {count} 个寄存器，实际只有 {registers.Length} 个", nameof(registers));

        // 第 1 步：Modbus 规定单个寄存器内部是“高字节在前”，先摊平成字节数组。
        // stackalloc 表示在栈上开数组，数据小、用完自动回收，比 new byte[] 更快。
        Span<byte> raw = stackalloc byte[count * 2];
        for (var i = 0; i < count; i++)
        {
            raw[i * 2] = (byte)(registers[i] >> 8);      // 高字节
            raw[i * 2 + 1] = (byte)(registers[i] & 0xFF); // 低字节
        }

        // 第 2 步：按现场设备实际的字序，重排成标准的 ABCD 顺序
        Span<byte> canonical = stackalloc byte[count * 2];
        Reorder(raw, canonical, order);

        // 第 3 步：把标准顺序的字节拼成具体类型。
        // 移位运算就是“把字节挪到它该在的位置”，例如 0x12 << 8 变成 0x1200。
        return type switch
        {
            RegisterDataType.UInt16 => (ushort)((canonical[0] << 8) | canonical[1]),
            RegisterDataType.Int16 => (short)((canonical[0] << 8) | canonical[1]),
            RegisterDataType.UInt32 => ((uint)canonical[0] << 24) | ((uint)canonical[1] << 16) | ((uint)canonical[2] << 8) | canonical[3],
            RegisterDataType.Int32 => (int)(((uint)canonical[0] << 24) | ((uint)canonical[1] << 16) | ((uint)canonical[2] << 8) | canonical[3]),
            RegisterDataType.Float32 => BitConverter.Int32BitsToSingle(
                (canonical[0] << 24) | (canonical[1] << 16) | (canonical[2] << 8) | canonical[3]),
            RegisterDataType.Float64 => BitConverter.Int64BitsToDouble(
                ((long)canonical[0] << 56) | ((long)canonical[1] << 48) | ((long)canonical[2] << 40) | ((long)canonical[3] << 32) |
                ((long)canonical[4] << 24) | ((long)canonical[5] << 16) | ((long)canonical[6] << 8) | canonical[7]),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }

    /// <summary>
    /// 把数值编码为寄存器数组（写 PLC 用）。
    /// 思路和 Decode 相反：先把数字拆成标准 ABCD 字节，再按现场字序还原，最后合并成寄存器。
    /// </summary>
    public static ushort[] Encode(double value, RegisterDataType type, RegisterOrder order = RegisterOrder.Abcd)
    {
        var count = RegisterCount(type);
        Span<byte> canonical = stackalloc byte[count * 2];

        // 第 1 步：把 double 转成目标类型，再拆成“标准顺序”的字节
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

        // 第 2 步：按设备字序重排字节
        Span<byte> raw = stackalloc byte[count * 2];
        Reorder(canonical, raw, order);

        // 第 3 步：每两个字节合并成一个 ushort（高字节左移 8 位，再和低字节做按位或）
        var result = new ushort[count];
        for (var i = 0; i < count; i++)
            result[i] = (ushort)((raw[i * 2] << 8) | raw[i * 2 + 1]);
        return result;
    }

    /// <summary>
    /// 按字序在两组字节之间搬运。
    /// map 里写的是“目标第 i 个字节取自源的第几个字节”。
    /// 四种字序的置换都是自逆的（再换一次就还原），所以编码和解码可以共用这一个方法。
    /// </summary>
    private static void Reorder(ReadOnlySpan<byte> source, Span<byte> target, RegisterOrder order)
    {
        var map = order switch
        {
            RegisterOrder.Abcd => new[] { 0, 1, 2, 3 }, // 标准顺序，不用动
            RegisterOrder.Badc => new[] { 1, 0, 3, 2 }, // 每个字内部两个字节交换
            RegisterOrder.Cdab => new[] { 2, 3, 0, 1 }, // 两个字整体交换
            RegisterOrder.Dcba => new[] { 3, 2, 1, 0 }, // 完全倒序
            _ => throw new ArgumentOutOfRangeException(nameof(order))
        };

        // i % 4 处理“当前是 4 字节组里的第几个”，i / 4 处理“现在是第几组”（Float64 有 2 组）
        for (var i = 0; i < source.Length; i++)
            target[i] = source[map[i % 4] + (i / 4) * 4];
    }

    /// <summary>字节交换，调试 16 位字序问题时常用。</summary>
    public static ushort SwapBytes(ushort value) => (ushort)((value >> 8) | (value << 8));

    /// <summary>
    /// 按缩放系数把原始值换算成工程值：工程值 = 原始值 * scale + offset。
    /// 例：寄存器读到 253，scale = 0.1，offset = 0，则实际温度是 25.3 ℃。
    /// </summary>
    public static double ToEngineering(double raw, double scale, double offset) => raw * scale + offset;

    /// <summary>
    /// 工程值反算寄存器原始值（写设定值时用）。
    /// 公式是 ToEngineering 的逆运算；scale 接近 0 时做特殊处理，避免除以 0。
    /// </summary>
    public static double ToRaw(double engineering, double scale, double offset)
        => Math.Abs(scale) < 1e-12 ? engineering - offset : (engineering - offset) / scale;
}
