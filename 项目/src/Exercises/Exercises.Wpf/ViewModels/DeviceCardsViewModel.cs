using System.Collections.ObjectModel;
using System.Windows.Input;
using Exercises.Wpf.Models;

namespace Exercises.Wpf.ViewModels;

/// <summary>
/// 第 05 章练习 3：设备卡片的 ViewModel。
///
/// 关键点：集合的类型是 ObservableCollection&lt;DeviceCardBase&gt;，
/// 里面同时装 PLC、电表、变频器三种子类对象。
/// 界面用 ItemsControl 展示，每一项由 DeviceTemplateSelector 按真实类型挑模板：
///   PlcCard → PLC 卡片
///   MeterCard → 电表卡片
///   InverterCard → 变频器卡片
///
/// 这就是“同一份数据，不同外观”的常见做法，比在 XAML 里写一堆可见性判断更清晰。
/// </summary>
public sealed class DeviceCardsViewModel : ObservableObject
{
    private readonly Random _random = new();
    private string _lastRefreshText;

    public DeviceCardsViewModel()
    {
        // 初始化 6 台演示设备。第 5 台（索引 4）设为离线，用来展示离线样式。
        Devices.Add(new PlcCard("1# 主控 PLC", "S7-1500 · 192.168.1.10", "1# 炉控制柜", 38.6, 6.2));
        Devices.Add(new MeterCard("进线总电表", "PMC-53M · Modbus TCP", "低压配电室", 382.4, 126.8, 0.96));
        Devices.Add(new InverterCard("循环泵变频器", "ACS580 · 192.168.1.31", "循环水泵房", 45.2, 38.6, "正转"));
        Devices.Add(new PlcCard("2# 安全 PLC", "S7-1200 · 192.168.1.12", "2# 炉控制柜", 21.4, 4.8));
        Devices.Add(new MeterCard("2# 泵电表", "PMC-53M · 192.168.1.42", "循环水泵房", 0, 0, 0));
        Devices.Add(new InverterCard("补水泵变频器", "ACS380 · 192.168.1.33", "补水间", 32.0, 12.4, "正转"));

        Devices[4].IsOnline = false;

        RefreshCommand = new RelayCommand(RefreshValues);
        _lastRefreshText = $"最后刷新 {DateTime.Now:HH:mm:ss}";
    }

    /// <summary>设备列表。基类集合可以装任意子类，界面按类型选模板。</summary>
    public ObservableCollection<DeviceCardBase> Devices { get; } = new();

    /// <summary>在线台数。Count 带 Lambda 条件，统计满足条件的设备。</summary>
    public string OnlineText => $"在线 {Devices.Count(device => device.IsOnline)} / {Devices.Count} 台";

    public string LastRefreshText
    {
        get => _lastRefreshText;
        private set => SetProperty(ref _lastRefreshText, value);
    }

    public ICommand RefreshCommand { get; }

    /// <summary>模拟刷新：按设备真实类型分别更新自己的专属字段。</summary>
    private void RefreshValues()
    {
        foreach (var device in Devices)
        {
            // switch 类型模式：匹配到具体子类后，变量 plc/meter/inverter 就是强类型，能直接访问专属属性。
            switch (device)
            {
                case PlcCard plc:
                    plc.CpuLoad = Math.Round(15 + _random.NextDouble() * 55, 1);
                    plc.ScanMs = Math.Round(3 + _random.NextDouble() * 6, 1);
                    break;
                case MeterCard meter:
                    meter.Voltage = Math.Round(378 + _random.NextDouble() * 8, 1);
                    meter.Current = Math.Round(105 + _random.NextDouble() * 40, 1);
                    meter.PowerFactor = Math.Round(0.92 + _random.NextDouble() * 0.06, 2);
                    break;
                case InverterCard inverter:
                    inverter.Frequency = Math.Round(30 + _random.NextDouble() * 20, 1);
                    inverter.MotorCurrent = Math.Round(24 + _random.NextDouble() * 18, 1);
                    inverter.Direction = _random.Next(0, 12) == 0 ? "待机" : "正转";
                    break;
            }
        }

        LastRefreshText = $"最后刷新 {DateTime.Now:HH:mm:ss}";
        OnPropertyChanged(nameof(OnlineText));
    }
}
