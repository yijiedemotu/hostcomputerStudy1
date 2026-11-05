namespace Industrial.Core.Modbus;

/// <summary>
/// Modbus 协议异常：从站明确返回了“我处理不了”的错误帧。
/// 错误帧的格式是 [从站号][功能码 | 0x80][异常码][CRC]，
/// 例如 01 83 02 ... 表示“1 号站执行 03 功能时地址非法”。
/// 这类异常说明通信链路是通的，问题在地址/功能码/数据，不要盲目重连。
/// </summary>
public sealed class ModbusException : Exception
{
    /// <summary>原始功能码（未加 0x80 的那个）。</summary>
    public byte FunctionCode { get; }

    /// <summary>异常码，含义见 <see cref="Describe"/>。</summary>
    public byte ExceptionCode { get; }

    public ModbusException(byte functionCode, byte exceptionCode)
        : base($"Modbus 异常：功能码 0x{functionCode:X2}，异常码 0x{exceptionCode:X2}（{Describe(exceptionCode)}）")
    {
        FunctionCode = functionCode;
        ExceptionCode = exceptionCode;
    }

    /// <summary>把异常码翻译成现场能看懂的排查提示。</summary>
    public static string Describe(byte code) => code switch
    {
        0x01 => "非法功能码：从站不支持该功能，检查 03/04/06/10 是否用对",
        0x02 => "非法数据地址：寄存器地址超出从站范围，注意 0/1 基址和“站内地址”差异",
        0x03 => "非法数据值：写入值或数量不合法",
        0x04 => "从站设备故障：从站内部错误",
        0x05 => "确认（已接受，处理中）",
        0x06 => "从站忙：稍后重试",
        0x08 => "存储奇偶校验错误",
        0x0A => "网关路径不可用",
        0x0B => "网关目标设备无响应：检查从站号、接线、终端电阻",
        _ => "未知异常"
    };
}

/// <summary>通信超时：在设定时间内没收到完整回帧，通常是掉线、站号错或 485 接线问题。</summary>
public sealed class ModbusTimeoutException : Exception
{
    public ModbusTimeoutException(string message) : base(message) { }
}

/// <summary>通信过程中的帧错误（CRC 错、长度错、从站号不匹配等），属于“收到了但内容不对”。</summary>
public sealed class ModbusProtocolException : Exception
{
    public ModbusProtocolException(string message) : base(message) { }
}
