using Answers.Wpf.ViewModels;

namespace Answers.Wpf.Models;

/// <summary>
/// 所有设备卡片的基类：三种设备共有的字段都放在这里。
///
/// 继承的好处：
/// - PLC、电表、变频器不用各自重复写 Name / Code / Location / IsOnline；
/// - 它们可以统一装进 ObservableCollection&lt;DeviceCardBase&gt;；
/// - 界面按具体子类型选择不同模板（见 DeviceTemplateSelector）。
///
/// 继承 ObservableObject 是为了 IsOnline 变化时能通知界面。
/// </summary>
public abstract class DeviceCardBase : ObservableObject
{
    private bool _isOnline = true;

    protected DeviceCardBase(string name, string code, string location, string kindLabel)
    {
        Name = name;
        Code = code;
        Location = location;
        KindLabel = kindLabel;
    }

    // 这几个属性在构造时确定，之后不会变，所以只有 get，没有 set。
    public string Name { get; }
    public string Code { get; }
    public string Location { get; }
    public string KindLabel { get; }

    /// <summary>是否在线。离线时界面会显示灰色/离线标记。</summary>
    public bool IsOnline
    {
        get => _isOnline;
        set => SetProperty(ref _isOnline, value);
    }
}

/// <summary>PLC 卡片：额外显示 CPU 负载和扫描周期。</summary>
public sealed class PlcCard : DeviceCardBase
{
    private double _cpuLoad;
    private double _scanMs;

    public PlcCard(string name, string code, string location, double cpuLoad, double scanMs)
        : base(name, code, location, "PLC")
    {
        _cpuLoad = cpuLoad;
        _scanMs = scanMs;
    }

    /// <summary>CPU 负载百分比。</summary>
    public double CpuLoad
    {
        get => _cpuLoad;
        set => SetProperty(ref _cpuLoad, value);
    }

    /// <summary>PLC 扫描周期（毫秒）。</summary>
    public double ScanMs
    {
        get => _scanMs;
        set => SetProperty(ref _scanMs, value);
    }
}

/// <summary>电表卡片：显示电压、电流、功率因数。</summary>
public sealed class MeterCard : DeviceCardBase
{
    private double _voltage;
    private double _current;
    private double _powerFactor;

    public MeterCard(string name, string code, string location, double voltage, double current, double powerFactor)
        : base(name, code, location, "电表")
    {
        _voltage = voltage;
        _current = current;
        _powerFactor = powerFactor;
    }

    public double Voltage
    {
        get => _voltage;
        set => SetProperty(ref _voltage, value);
    }

    public double Current
    {
        get => _current;
        set => SetProperty(ref _current, value);
    }

    /// <summary>功率因数，通常 0~1，越接近 1 越好。</summary>
    public double PowerFactor
    {
        get => _powerFactor;
        set => SetProperty(ref _powerFactor, value);
    }
}

/// <summary>变频器卡片：显示频率、电机电流、转向。</summary>
public sealed class InverterCard : DeviceCardBase
{
    private double _frequency;
    private double _motorCurrent;
    private string _direction;

    public InverterCard(string name, string code, string location, double frequency, double motorCurrent, string direction)
        : base(name, code, location, "变频器")
    {
        _frequency = frequency;
        _motorCurrent = motorCurrent;
        _direction = direction;
    }

    public double Frequency
    {
        get => _frequency;
        set => SetProperty(ref _frequency, value);
    }

    public double MotorCurrent
    {
        get => _motorCurrent;
        set => SetProperty(ref _motorCurrent, value);
    }

    /// <summary>转向文字：正转 / 反转 / 待机。</summary>
    public string Direction
    {
        get => _direction;
        set => SetProperty(ref _direction, value);
    }
}
