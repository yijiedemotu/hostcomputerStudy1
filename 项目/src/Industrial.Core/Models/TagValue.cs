namespace Industrial.Core.Models;

/// <summary>
/// 数据质量。上位机必须区分“值为 0”和“没采到”，
/// 否则通信中断时界面全显示 0，会误报警、误入库。
/// </summary>
public enum Quality
{
    /// <summary>采集正常。</summary>
    Good,

    /// <summary>数据可疑（例如超量程、偶尔一次校验失败）。</summary>
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
/// 一次采集得到的点值快照。
/// 界面每次刷新、写历史库、判断报警，传递的都是这个对象。
/// </summary>
public sealed class TagValue
{
    /// <summary>设备名，来自配置。</summary>
    public required string DeviceName { get; init; }

    /// <summary>点位名，来自配置。</summary>
    public required string TagName { get; init; }

    /// <summary>换算后的工程值（已经乘过 Scale、加过 Offset）。</summary>
    public double Value { get; set; }

    /// <summary>单位。</summary>
    public string Unit { get; init; } = "";

    /// <summary>采样时间，默认取当前时间。</summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>数据质量，默认正常。</summary>
    public Quality Quality { get; set; } = Quality.Good;

    /// <summary>报警级别，默认不报警。</summary>
    public AlarmLevel Alarm { get; set; } = AlarmLevel.None;

    /// <summary>采集失败时的错误信息；正常时为 null。</summary>
    public string? Error { get; set; }

    /// <summary>计算属性：只要级别不是 None 就算报警。</summary>
    public bool IsAlarm => Alarm != AlarmLevel.None;

    /// <summary>把质量枚举翻译成中文，给界面绑定用。</summary>
    public string QualityText => Quality switch
    {
        Quality.Good => "正常",
        Quality.Uncertain => "可疑",
        _ => "故障"
    };
}

/// <summary>
/// 写进历史库的一行。
/// 用 record 是因为它只用来“存下来、传出去”，不需要改；
/// record 自带值相等比较和更易读的 ToString。
/// </summary>
public sealed record HistoryRecord(
    DateTime Timestamp,
    string Device,
    string Tag,
    double Value,
    string Unit,
    Quality Quality,
    AlarmLevel Alarm);
