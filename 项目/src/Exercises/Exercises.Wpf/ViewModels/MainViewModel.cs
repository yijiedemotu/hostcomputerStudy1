using System.Collections.ObjectModel;

namespace Exercises.Wpf.ViewModels;

/// <summary>
/// 左侧导航的一项：编号、标题、副标题、说明，以及右侧要显示的内容对象。
/// Content 的类型是 object，所以既能放 AlarmSoundViewModel，也能放 ThemeViewModel。
/// </summary>
public sealed class SectionItem
{
    public SectionItem(string number, string title, string subtitle, string description, object content)
    {
        Number = number;
        Title = title;
        Subtitle = subtitle;
        Description = description;
        Content = content;
    }

    public string Number { get; }
    public string Title { get; }
    public string Subtitle { get; }
    public string Description { get; }

    /// <summary>右侧内容区显示的对象；由 XAML 的 ContentControl 绑定并自动选择视图。</summary>
    public object Content { get; }
}

/// <summary>
/// 主窗口的 ViewModel：管理左侧 5 个练习入口，以及当前选中的入口。
///
/// 注意它不引用任何 Window/Button：
/// 切换页面只改 SelectedSection，界面通过双向绑定自动跟着变。
/// 这就是 MVVM 中“ViewModel 不知道 View 长什么样”的体现。
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    private SectionItem _selectedSection;

    public MainViewModel()
    {
        Theme = new ThemeViewModel();

        // ObservableCollection 是“会通知界面”的集合：
        // 增删项时，绑定到它的 ListBox 会自动刷新。
        Sections = new ObservableCollection<SectionItem>
        {
            new("01", "报警声音", "SystemSounds · 静音 / 确认",
                "View 的定时器驱动 VM 更新数据，报警时播放系统提示音；VM 不引用 Dispatcher，界面和逻辑各管一层。",
                new AlarmSoundViewModel()),
            new("02", "趋势十字光标", "自绘控件 · OnRender",
                "自绘 FrameworkElement 画出网格、曲线和提示框；鼠标移动时反算最近的数据点，显示时间与数值。",
                new CrosshairViewModel()),
            new("03", "设备卡片", "DataTemplateSelector",
                "PLC、电表、变频器共用一组设备数据，由 DataTemplateSelector 按 VM 类型挑选完全不同的卡片模板。",
                new DeviceCardsViewModel()),
            new("04", "日夜主题", "ResourceDictionary · 换肤",
                "颜色全部抽到主题资源字典，界面用 DynamicResource 引用；运行时替换字典即可整窗换肤，无需重建控件。",
                Theme),
            new("05", "报警排序筛选", "CollectionViewSource · DataGrid",
                "CollectionViewSource 在 ObservableCollection 之上做过滤和排序，DataGrid 只负责显示视图。",
                new AlarmListViewModel())
        };

        _selectedSection = Sections[0];
    }

    /// <summary>主题 VM 单独暴露，因为左侧底部也有一个“切换主题”按钮绑定它。</summary>
    public ThemeViewModel Theme { get; }
    public ObservableCollection<SectionItem> Sections { get; }

    /// <summary>
    /// 当前选中的导航项。
    /// ListBox.SelectedItem 与它双向绑定：
    /// 用户点列表 → 更新这个属性；代码改这个属性 → 列表高亮跟着变。
    /// </summary>
    public SectionItem SelectedSection
    {
        get => _selectedSection;
        set => SetProperty(ref _selectedSection, value);
    }
}
