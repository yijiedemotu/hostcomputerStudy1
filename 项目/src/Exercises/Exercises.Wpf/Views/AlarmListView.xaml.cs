using System.Windows.Controls;

namespace Exercises.Wpf.Views;

/// <summary>
/// 第 05 章练习 5 的 View。
///
/// 筛选、排序、统计都在 AlarmListViewModel；
/// XAML 里的 DataGrid 直接绑定 AlarmView，下拉框/搜索框双向绑定筛选条件。
/// 后台代码只保留 InitializeComponent。
/// </summary>
public partial class AlarmListView : UserControl
{
    public AlarmListView() => InitializeComponent();
}
