namespace Industrial.Core.Transport;

/// <summary>校验位。工业现场 99% 用 None，老仪表常见 Even/Odd。</summary>
public enum ParityMode
{
    /// <summary>无校验。</summary>
    None = 0,

    /// <summary>奇校验。</summary>
    Odd = 1,

    /// <summary>偶校验。</summary>
    Even = 2,

    /// <summary>固定为 1（少用）。</summary>
    Mark = 3,

    /// <summary>固定为 0（少用）。</summary>
    Space = 4
}

/// <summary>停止位。</summary>
public enum StopBitsMode
{
    /// <summary>1 位停止位，最常用。</summary>
    One = 0,

    /// <summary>1.5 位停止位（仅部分老设备）。</summary>
    OnePointFive = 1,

    /// <summary>2 位停止位。</summary>
    Two = 2
}

/// <summary>
/// 串口参数。工业仪表最经典的组合是 9600, 8, None, 1（简称 9600-8-N-1），
/// 台达/汇川/西门子 200 SMART 的 RS485 口大多也是这个。
/// </summary>
public sealed class SerialSettings
{
    /// <summary>串口名，例如 COM1、COM3。USB 转 485 在设备管理器里能看到。</summary>
    public string PortName { get; set; } = "COM1";

    /// <summary>波特率，双方必须一致，否则收到的全是乱码。</summary>
    public int BaudRate { get; set; } = 9600;

    /// <summary>数据位，Modbus 固定 8。</summary>
    public int DataBits { get; set; } = 8;

    /// <summary>校验位。</summary>
    public ParityMode Parity { get; set; } = ParityMode.None;

    /// <summary>停止位。</summary>
    public StopBitsMode StopBits { get; set; } = StopBitsMode.One;

    /// <summary>读超时，单位毫秒。Modbus RTU 一帧通常 &lt; 100ms。</summary>
    public int ReadTimeoutMs { get; set; } = 200;

    /// <summary>生成 "COM3 9600,8,N,1" 这样的可读字符串，用于界面和日志。</summary>
    public override string ToString() =>
        $"{PortName} {BaudRate},{DataBits},{Parity.ToString()[0]},{StopBits switch
        {
            StopBitsMode.One => 1,
            StopBitsMode.OnePointFive => 1.5,
            _ => 2
        }}";
}
