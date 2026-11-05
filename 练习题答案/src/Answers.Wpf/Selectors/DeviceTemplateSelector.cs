using System.Windows;
using System.Windows.Controls;
using Answers.Wpf.Models;

namespace Answers.Wpf.Selectors;

/// <summary>
/// 根据数据对象的“真实类型”，挑选不同的 DataTemplate。
///
/// 题目 05-3 的核心：
/// 设备列表里同时有 PLC、电表、变频器三种对象。
/// 它们都继承 DeviceCardBase，可以放进同一个 ObservableCollection。
/// 但界面上要显示完全不同的卡片（PLC 显示 CPU/扫描周期，电表显示电压/电流……）。
///
/// DataTemplateSelector 在每一项渲染前被调用：
/// - 是 PlcCard → 返回 PlcTemplate；
/// - 是 MeterCard → 返回 MeterTemplate；
/// - 是 InverterCard → 返回 InverterTemplate。
///
/// 三个模板都在 Resources 里用 x:Key 定义，XAML 中再把本选择器指定给 ItemsControl。
/// </summary>
public sealed class DeviceTemplateSelector : DataTemplateSelector
{
    // 这三个属性由 XAML 赋值，必须声明为 public 属性，WPF 才能设置。
    public DataTemplate? PlcTemplate { get; set; }
    public DataTemplate? MeterTemplate { get; set; }
    public DataTemplate? InverterTemplate { get; set; }

    /// <summary>
    /// item 是当前要显示的数据对象，container 是承载它的界面容器。
    /// switch 模式匹配按类型分支；when 再确认模板确实存在，避免返回 null。
    /// </summary>
    public override DataTemplate SelectTemplate(object item, DependencyObject container) =>
        item switch
        {
            PlcCard when PlcTemplate is not null => PlcTemplate,
            MeterCard when MeterTemplate is not null => MeterTemplate,
            InverterCard when InverterTemplate is not null => InverterTemplate,
            _ => base.SelectTemplate(item, container)!
        };
}
