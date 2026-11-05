using System.Windows;
using Answers.Wpf.ViewModels;

namespace Answers.Wpf.Windows;

/// <summary>
/// 第 05 章练习 5 的窗口后台。
///
/// 筛选、排序、统计都在 AlarmListViewModel；
/// XAML 里的 DataGrid 直接绑定 AlarmView，搜索框和级别胶囊双向绑定筛选条件。
/// 后台代码只设置 DataContext。
/// </summary>
public partial class AlarmListWindow : Window
{
    public AlarmListWindow()
    {
        InitializeComponent();
        DataContext = new AlarmListViewModel();
    }
}
