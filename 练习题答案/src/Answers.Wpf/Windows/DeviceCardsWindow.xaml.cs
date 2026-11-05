using System.Windows;
using Answers.Wpf.ViewModels;

namespace Answers.Wpf.Windows;

/// <summary>
/// 第 05 章练习 3 的窗口后台。
///
/// 页面逻辑几乎都在 XAML 和 ViewModel 里：
/// - ItemsControl 绑定 Devices；
/// - ItemTemplateSelector 按设备类型选卡片模板；
/// - 按钮绑定 RefreshCommand。
/// 所以后台代码只需要设置 DataContext，这是 MVVM 理想状态：代码后置非常薄。
/// </summary>
public partial class DeviceCardsWindow : Window
{
    public DeviceCardsWindow()
    {
        InitializeComponent();
        DataContext = new DeviceCardsViewModel();
    }
}
