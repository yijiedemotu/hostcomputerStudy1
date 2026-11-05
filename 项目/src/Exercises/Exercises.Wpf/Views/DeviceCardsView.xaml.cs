using System.Windows.Controls;

namespace Exercises.Wpf.Views;

/// <summary>
/// 第 05 章练习 3 的 View。
///
/// 页面逻辑几乎都在 XAML 和 ViewModel 里：
/// - ItemsControl 绑定 Devices；
/// - ItemTemplateSelector 按设备类型选卡片模板；
/// - 按钮绑定 RefreshCommand。
/// 因此后台代码只需要 InitializeComponent，这是 MVVM 理想状态：代码后置非常薄。
/// </summary>
public partial class DeviceCardsView : UserControl
{
    public DeviceCardsView() => InitializeComponent();
}
