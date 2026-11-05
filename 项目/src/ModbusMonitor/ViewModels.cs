using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ModbusMonitor;

/// <summary>
/// 表格行（视图模型）。
/// 用 BindingList + INotifyPropertyChanged，后台采集线程改属性，界面自动刷新，
/// 不用一行行去改 DataGridView 的单元格——这是 WinForms 数据绑定的正确姿势。
/// </summary>
public sealed class TagRow : INotifyPropertyChanged
{
    // 会变化的值放在字段里；不变的信息（设备/位号/单位/地址）用只读属性
    private string _value = "--";
    private string _time = "";
    private string _quality = "未采集";
    private string _alarm = "";

    public TagRow(string device, string tag, string unit, string plcAddress)
    {
        Device = device;
        Tag = tag;
        Unit = unit;
        PlcAddress = plcAddress;
    }

    // [DisplayName] 决定 DataGridView 自动生成列时的表头文字
    [DisplayName("设备")]
    public string Device { get; }

    [DisplayName("位号")]
    public string Tag { get; }

    /// <summary>当前值。set 里调用 Set，属性变化时通知界面刷新。</summary>
    [DisplayName("当前值")]
    public string Value
    {
        get => _value;
        set => Set(ref _value, value);
    }

    [DisplayName("单位")]
    public string Unit { get; }

    [DisplayName("PLC 地址")]
    public string PlcAddress { get; }

    [DisplayName("更新时间")]
    public string Time
    {
        get => _time;
        set => Set(ref _time, value);
    }

    [DisplayName("质量")]
    public string Quality
    {
        get => _quality;
        set => Set(ref _quality, value);
    }

    [DisplayName("报警")]
    public string Alarm
    {
        get => _alarm;
        set => Set(ref _alarm, value);
    }

    // [Browsable(false)] 表示不在表格里显示，仅供界面着色逻辑使用
    [Browsable(false)]
    public bool IsBad { get; private set; }

    [Browsable(false)]
    public bool IsAlarm { get; private set; }

    /// <summary>
    /// 一次更新所有显示字段。
    /// 注意：这个方法由采集线程通过 UI Invoke 调用，所以能安全触发 PropertyChanged。
    /// </summary>
    public void Update(string value, DateTime time, string quality, string alarm, bool isBad, bool isAlarm)
    {
        Value = value;
        Time = time.ToString("HH:mm:ss.fff");
        Quality = quality;
        Alarm = alarm;
        IsBad = isBad;
        IsAlarm = isAlarm;

        // 这两个属性没有 set 访问器，只 Set 不会通知，所以手动通知
        OnPropertyChanged(nameof(IsBad));
        OnPropertyChanged(nameof(IsAlarm));
    }

    /// <summary>属性变化事件，DataGridView 靠它自动刷新单元格。</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 通用赋值：值没变就不通知（避免无谓刷新）；
    /// [CallerMemberName] 让编译器自动把属性名填进来，调用时不用手写。
    /// </summary>
    private void Set(ref string field, string value, [CallerMemberName] string? property = null)
    {
        if (field == value) return;
        field = value;
        OnPropertyChanged(property);
    }

    private void OnPropertyChanged(string? property) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
}

/// <summary>
/// 一条报警记录（只读）。
/// 报警表只追加不修改，所以所有属性都是 get-only，构造时一次填好。
/// </summary>
public sealed class AlarmRecord
{
    public AlarmRecord(DateTime time, string device, string tag, double value, string level, string action)
    {
        Time = time;
        Device = device;
        Tag = tag;
        Value = value;
        Level = level;
        Action = action;
    }

    [DisplayName("时间")]
    public DateTime Time { get; }

    [DisplayName("设备")]
    public string Device { get; }

    [DisplayName("位号")]
    public string Tag { get; }

    [DisplayName("数值")]
    public double Value { get; }

    [DisplayName("级别")]
    public string Level { get; }

    [DisplayName("动作")]
    public string Action { get; }
}
