using Industrial.Core.Modbus;
using Industrial.Core.Transport;

namespace Industrial.Core.Models;

/// <summary>
/// 一个采集点（也叫“位号 / 标签 / Tag”）。
/// 它描述“从哪个寄存器、按什么类型、怎么换算、报警范围是多少”，
/// 本身不存值；运行时采到的值放在 <see cref="TagValue"/> 里。
/// </summary>
public sealed class PointConfig
{
    /// <summary>显示名称，例如“1#炉 炉温”。</summary>
    public string Name { get; set; } = "";

    /// <summary>寄存器地址（0 基址，直接填报文里的地址）。</summary>
    public ushort Address { get; set; }

    /// <summary>PLC/仪表的地址写法，仅用于显示和对照手册，例如 “40001”“D100”。</summary>
    public string? PlcAddress { get; set; }

    /// <summary>数据类型，决定占几个寄存器、怎么解释这串字节。</summary>
    public RegisterDataType DataType { get; set; } = RegisterDataType.Int16;

    /// <summary>32/64 位数据的字序，必须和设备手册一致。</summary>
    public RegisterOrder Order { get; set; } = RegisterOrder.Abcd;

    /// <summary>工程值 = 原始值 * Scale + Offset。温度常见 0.1，压力常见 0.01。</summary>
    public double Scale { get; set; } = 1;

    /// <summary>零点偏移，一般不用；有些仪表用 4mA 对应负值时才需要。</summary>
    public double Offset { get; set; }

    /// <summary>单位，显示用，例如 “℃”“MPa”。</summary>
    public string Unit { get; set; } = "";

    /// <summary>下限，null 表示不设下限。</summary>
    public double? LowLimit { get; set; }

    /// <summary>上限，null 表示不设上限。</summary>
    public double? HighLimit { get; set; }

    /// <summary>是否启用该采集点（调试时可以先关掉个别点）。</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>备注，例如“量程 0~300℃”。</summary>
    public string? Description { get; set; }
}

/// <summary>设备使用的协议。</summary>
public enum DeviceProtocol
{
    /// <summary>Modbus TCP：走网口，默认端口 502。</summary>
    ModbusTcp,

    /// <summary>Modbus RTU：走 RS485/RS232 串口。</summary>
    ModbusRtu
}

/// <summary>
/// 一台设备（一个 IP:502，或一个 COM 口上的一台从站）。
/// 一个设备下面挂多个采集点，采集时按 PollIntervalMs 周期轮询所有点。
/// </summary>
public sealed class DeviceConfig
{
    public string Name { get; set; } = "";

    /// <summary>关闭后该设备不参与采集（调试单台设备时很方便）。</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>通信协议：TCP 还是 RTU。</summary>
    public DeviceProtocol Protocol { get; set; } = DeviceProtocol.ModbusTcp;

    /// <summary>从站号（站地址），RTU 必填，TCP 也常填 1。</summary>
    public byte SlaveId { get; set; } = 1;

    // ---- 以下字段仅在 ModbusTcp 时有效 ----

    /// <summary>PLC/网关的 IP 地址。</summary>
    public string Host { get; set; } = "127.0.0.1";

    /// <summary>端口，Modbus TCP 标准端口是 502。</summary>
    public int Port { get; set; } = 502;

    // ---- 以下字段仅在 ModbusRtu 时有效 ----

    /// <summary>串口名，例如 COM1、COM3。</summary>
    public string PortName { get; set; } = "COM1";

    /// <summary>波特率，必须和现场设备一致（常见 9600、19200、38400）。</summary>
    public int BaudRate { get; set; } = 9600;

    /// <summary>数据位，Modbus 几乎都是 8。</summary>
    public int DataBits { get; set; } = 8;

    /// <summary>校验方式：无 / 奇 / 偶，必须和设备一致。</summary>
    public ParityMode Parity { get; set; } = ParityMode.None;

    /// <summary>停止位，通常是 1 位。</summary>
    public StopBitsMode StopBits { get; set; } = StopBitsMode.One;

    /// <summary>采集周期（毫秒）。现场一般 200~1000ms，太快会把 485 总线拖死。</summary>
    public int PollIntervalMs { get; set; } = 500;

    /// <summary>这台设备下要采集的所有点。</summary>
    public List<PointConfig> Points { get; set; } = new();

    /// <summary>给日志和界面显示用的连接描述，只读。</summary>
    public string Endpoint => Protocol == DeviceProtocol.ModbusTcp
        ? $"{Host}:{Port}"
        : $"{PortName}@{BaudRate}";

    /// <summary>把 RTU 相关字段组装成串口参数对象。</summary>
    public SerialSettings ToSerialSettings() => new()
    {
        PortName = PortName,
        BaudRate = BaudRate,
        DataBits = DataBits,
        Parity = Parity,
        StopBits = StopBits
    };
}

/// <summary>
/// 整个上位机的配置，序列化为 config/appsettings.json。
/// 程序启动时读入，界面上修改后再保存回去。
/// </summary>
public sealed class AppConfig
{
    /// <summary>窗口标题。</summary>
    public string Title { get; set; } = "工业数据采集上位机";

    /// <summary>历史数据目录（相对于程序运行目录）。</summary>
    public string DataDirectory { get; set; } = "data";

    /// <summary>所有设备配置。</summary>
    public List<DeviceConfig> Devices { get; set; } = new();
}
