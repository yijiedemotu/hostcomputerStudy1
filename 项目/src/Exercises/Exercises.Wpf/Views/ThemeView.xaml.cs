using System.Windows.Controls;

namespace Exercises.Wpf.Views;

/// <summary>
/// 第 05 章练习 4 的 View。
///
/// 换肤逻辑在 ThemeViewModel + ThemeManager，颜色定义在 Themes/*.xaml。
/// 本页面只负责用 XAML 排版按钮和色卡，因此后台代码为空。
/// </summary>
public partial class ThemeView : UserControl
{
    public ThemeView() => InitializeComponent();
}
