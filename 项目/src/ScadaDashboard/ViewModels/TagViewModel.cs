using Industrial.Core.Models;

namespace ScadaDashboard.ViewModels;

/// <summary>
/// 一个位号在界面上的视图模型：值、质量、报警状态。
///
/// 采集线程只调用 <see cref="Update"/>，XAML 通过绑定自动刷新——不用碰任何控件。
/// 注意它不引用任何 WPF 类型，所以即使没有界面，这个类也能单独测试。
/// </summary>
public sealed class TagViewModel : ObservableObject
{
    // 下面这些私有字段就是界面真正绑定的数据源。
    // 初始值故意写成“--”“未采集”，这样程序刚启动、还没采到数时界面也不会是空白。
    private string _valueText = "--";
    private string _quality = "未采集";
    private string _time = "";
    private bool _isAlarm;
    private bool _isBad;

    /// <summary>
    /// 构造时把配置里的位号信息固定下来。
    /// Device/Name/Unit/上下限在运行期间不会变，所以做成只读属性。
    /// </summary>
    public TagViewModel(string device, string name, string unit, double? lowLimit, double? highLimit)
    {
        Device = device;
        Name = name;
        Unit = unit;
        LowLimit = lowLimit;
        HighLimit = highLimit;
    }

    // ---- 以下五个是“静态信息”，来自配置文件，只读 ----

    /// <summary>设备名，例如“1#注塑机”。</summary>
    public string Device { get; }

    /// <summary>位号名，例如“模温”。</summary>
    public string Name { get; }

    /// <summary>工程单位，例如“℃”。</summary>
    public string Unit { get; }

    /// <summary>下限（可为空，表示不检查）。</summary>
    public double? LowLimit { get; }

    /// <summary>上限（可为空，表示不检查）。</summary>
    public double? HighLimit { get; }

    /// <summary>
    /// 唯一键：设备名 + 位号。
    /// 两个设备可能有同名位号，所以不能只用 Name 当键。
    /// </summary>
    public string Key => Device + "|" + Name;

    // ---- 以下五个是“动态信息”，采集时更新，界面靠它们刷新 ----

    /// <summary>显示用的数值文本，保留最多 3 位小数。</summary>
    public string ValueText
    {
        get => _valueText;
        private set => Set(ref _valueText, value);   // private set：只允许本类 Update 修改
    }

    /// <summary>通讯质量文本：良好 / 未采集 / 坏值。</summary>
    public string Quality
    {
        get => _quality;
        private set => Set(ref _quality, value);
    }

    /// <summary>本次采样的时间，只显示时分秒。</summary>
    public string Time
    {
        get => _time;
        private set => Set(ref _time, value);
    }

    /// <summary>是否处于报警状态，XAML 的 DataTrigger 用它把卡片变红。</summary>
    public bool IsAlarm
    {
        get => _isAlarm;
        private set => Set(ref _isAlarm, value);
    }

    /// <summary>通讯是否失败（坏值），XAML 用它把卡片变灰。</summary>
    public bool IsBad
    {
        get => _isBad;
        private set => Set(ref _isBad, value);
    }

    /// <summary>
    /// 用一次新的采集结果刷新界面数据。
    /// 这里只改属性，不碰控件——WPF 绑定会替我们完成刷新。
    /// </summary>
    public void Update(TagValue value)
    {
        // 0.### 表示最多 3 位小数，末尾多余的 0 不显示（12.50 → 12.5）。
        ValueText = value.Value.ToString("0.###");
        Quality = value.QualityText;
        Time = value.Timestamp.ToString("HH:mm:ss");

        // Bad 表示这次通讯没读到有效数据，界面显示成灰色。
        IsBad = value.Quality == Industrial.Core.Models.Quality.Bad;

        // IsAlarm 由核心库根据上下限算好，界面只管显示。
        IsAlarm = value.IsAlarm;
    }
}

/// <summary>
/// 报警列表里的一行（DataGrid 直接绑定它的属性）。
///
/// 报警记录一旦产生就不应该再改，所以所有属性都是只读的，
/// 构造时一次性填好。这样也避免了“历史报警被后面采集覆盖”的 bug。
/// </summary>
public sealed class AlarmItem
{
    /// <summary>
    /// 创建一条报警记录。
    /// time：发生时间；tagName：“设备/位号”；value：当时的数值；
    /// levelText：“超上限/低于下限”；actionText：“触发/恢复”。
    /// </summary>
    public AlarmItem(DateTime time, string tagName, double value, string levelText, string actionText)
    {
        Time = time;
        TagName = tagName;
        ValueText = value.ToString("0.###");   // 数值提前格式化成文本
        LevelText = levelText;
        ActionText = actionText;
    }

    public DateTime Time { get; }
    public string TagName { get; }
    public string ValueText { get; }
    public string LevelText { get; }
    public string ActionText { get; }
}
