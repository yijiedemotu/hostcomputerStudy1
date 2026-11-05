using System.Windows;
using Answers.Wpf.ViewModels;

namespace Answers.Wpf.Windows;

/// <summary>
/// 第 05 章练习 4 的窗口后台。
///
/// 换肤逻辑在 ThemeViewModel + ThemeManager，颜色定义在 Themes/*.xaml。
/// 本窗口只负责用 XAML 排版按钮和色卡，因此后台代码只设置 DataContext。
/// </summary>
public partial class ThemeWindow : Window
{
    public ThemeWindow()
    {
        InitializeComponent();
        DataContext = new ThemeViewModel();
    }
}
