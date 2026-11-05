using Answers.ConsoleApp.Modbus;

namespace Answers.ConsoleApp.Models;

/// <summary>
/// 数据质量：上位机必须区分“值真的是 0”和“根本没采到”。
/// 通信中断时如果界面全显示 0，会误导操作员，也可能误入库。
/// </summary>
public enum Quality
{
    /// <summary>采集正常，数据可信。</summary>
    Good,

    /// <summary>数据可疑，例如偶发校验失败、超量程。</summary>
    Uncertain,

    /// <summary>采集失败，值不可信。</summary>
    Bad
}

/// <summary>报警级别。None 表示正常。</summary>
public enum AlarmLevel
{
    None,
    Low,
    High
}

/// <summary>
/// 一个运行时位号值：设备名 + 位号名 + 数值 + 单位 + 质量 + 报警级别。
/// 控制台练习只用它来演示 LINQ 筛选。
/// </summary>
public sealed class TagValue
{
    public string DeviceName { get; set; } = "";
    public string TagName { get; set; } = "";
    public double Value { get; set; }
    public string Unit { get; set; } = "";
    public Quality Quality { get; set; } = Quality.Good;
    public AlarmLevel Alarm { get; set; } = AlarmLevel.None;
}

/// <summary>
/// 一条历史记录。第 10 章用 Channel 在采集端和存盘端之间传递它。
/// record 会自动生成构造函数、属性、相等比较，适合这种纯数据对象。
/// </summary>
public sealed record HistoryRecord(
    DateTime Time,
    string Device,
    string Tag,
    double Value,
    string Unit,
    Quality Quality,
    AlarmLevel Alarm);

/// <summary>设备通信协议。本例只区分 Modbus TCP / RTU，便于配置阅读。</summary>
public enum DeviceProtocol
{
    ModbusTcp,
    ModbusRtu
}

/// <summary>
/// 一个采集点（位号）的配置。
/// 真实项目会从 appsettings.json 读取；本答案为了独立运行，直接在代码里构造。
/// </summary>
public sealed class PointConfig
{
    public string Name { get; set; } = "";
    public ushort Address { get; set; }
    public RegisterDataType DataType { get; set; } = RegisterDataType.Int16;
    public double Scale { get; set; } = 1;
    public string Unit { get; set; } = "";
}

/// <summary>一台设备的配置。</summary>
public sealed class DeviceConfig
{
    public string Name { get; set; } = "";
    public DeviceProtocol Protocol { get; set; } = DeviceProtocol.ModbusTcp;
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 502;
    public byte SlaveId { get; set; } = 1;

    /// <summary>采集周期（毫秒）。第 07 章练习 5 比较的就是这个值。</summary>
    public int PollIntervalMs { get; set; } = 500;

    public List<PointConfig> Points { get; } = new();
}

/// <summary>整个程序的采集配置，对应 appsettings.json 的根对象。</summary>
public sealed class AppConfig
{
    public List<DeviceConfig> Devices { get; } = new();
}
