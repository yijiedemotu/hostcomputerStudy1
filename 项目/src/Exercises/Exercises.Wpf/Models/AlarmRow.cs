namespace Exercises.Wpf.Models;

/// <summary>
/// 报警列表的一行数据。
///
/// init 表示“对象创建后不能再改”，适合这种一次生成、只读展示的记录。
/// 如果需要在界面上编辑并刷新，就要改成 set 并实现 INotifyPropertyChanged。
///
/// AckText 是“计算属性”：没有自己的字段，每次读取都根据 Acknowledged 算出来。
/// 注意：如果 Acknowledged 之后会变化，需要手动发 PropertyChanged 通知 AckText。
/// </summary>
public sealed class AlarmRow
{
    public DateTime Time { get; init; }
    public string Device { get; init; } = "";
    public string Tag { get; init; } = "";

    /// <summary>严重程度：报警 / 故障 / 提示。</summary>
    public string Severity { get; init; } = "提示";

    public double Value { get; init; }
    public string Message { get; init; } = "";
    public bool Acknowledged { get; init; }

    /// <summary>给界面显示的中文状态。</summary>
    public string AckText => Acknowledged ? "已确认" : "未确认";
}
