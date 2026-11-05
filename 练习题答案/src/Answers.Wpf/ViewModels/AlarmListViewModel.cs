using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using Answers.Wpf.Models;

namespace Answers.Wpf.ViewModels;

/// <summary>
/// 第 05 章练习 5：报警列表的排序与筛选。
///
/// 重点概念：ICollectionView（集合视图）。
/// - Alarms 是“全量数据”，ObservableCollection，增删会通知界面；
/// - AlarmView 是在 Alarms 之上的一层“视图”：决定哪些能看见、按什么顺序显示；
/// - DataGrid 绑定 AlarmView，所以筛选/排序只影响显示，不修改原始数据。
///
/// 三个筛选条件：
/// 1. 严重程度：全部 / 报警 / 故障 / 提示；
/// 2. 搜索关键字：在设备名、位号、说明里查找（不区分大小写）；
/// 3. 排序：时间倒序 / 时间正序 / 按设备名。
///
/// 条件改变时调用 _alarmView.Refresh()，视图会重新执行 Filter 和排序。
/// </summary>
public sealed class AlarmListViewModel : ObservableObject
{
    private readonly Random _random = new();

    // CollectionViewSource.GetDefaultView(Alarms) 返回与 Alarms 关联的默认视图。
    private readonly ICollectionView _alarmView;

    private string _selectedSeverity = "全部";
    private string _selectedSort = "时间：最新在前";
    private string _searchText = "";

    private static readonly string[] Devices = { "1# 炉", "2# 炉", "循环水泵", "空压机", "低压配电" };
    private static readonly string[] Tags = { "炉膛温度", "主蒸汽压力", "给水流量", "电机电流", "烟气含氧量", "出口压力" };

    public AlarmListViewModel()
    {
        SeedAlarms();
        AddAlarmCommand = new RelayCommand(AddSampleAlarm);

        _alarmView = CollectionViewSource.GetDefaultView(Alarms);

        // Filter 是一个“每项都会被问一次”的委托：返回 true 显示，false 隐藏。
        _alarmView.Filter = FilterAlarm;
        ApplySort();
    }

    /// <summary>全量报警数据，界面不直接绑定它，而是绑定下面的 AlarmView。</summary>
    public ObservableCollection<AlarmRow> Alarms { get; } = new();

    /// <summary>带筛选/排序的视图，DataGrid 绑定这个。</summary>
    public ICollectionView AlarmView => _alarmView;

    // 下拉框的选项。IReadOnlyList 表示界面只能读，不能改集合结构。
    public IReadOnlyList<string> SeverityOptions { get; } = new[] { "全部", "报警", "故障", "提示" };
    public IReadOnlyList<string> SortOptions { get; } = new[] { "时间：最新在前", "时间：最早在前", "按设备名称" };

    /// <summary>
    /// 严重程度筛选。变更后 Refresh 视图，并通知“显示 x / 共 y 条”的统计文字更新。
    /// </summary>
    public string SelectedSeverity
    {
        get => _selectedSeverity;
        set
        {
            if (!SetProperty(ref _selectedSeverity, value)) return;
            _alarmView.Refresh();
            OnPropertyChanged(nameof(CountText));
        }
    }

    /// <summary>排序方式。排序规则不是 Filter，要重新设置 SortDescriptions。</summary>
    public string SelectedSort
    {
        get => _selectedSort;
        set
        {
            if (!SetProperty(ref _selectedSort, value)) return;
            ApplySort();
            OnPropertyChanged(nameof(CountText));
        }
    }

    /// <summary>搜索框文字。每输入一个字符就会触发 Filter 重新计算。</summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!SetProperty(ref _searchText, value)) return;
            _alarmView.Refresh();
            OnPropertyChanged(nameof(CountText));
        }
    }

    /// <summary>
    /// 统计文字。AlarmView.Cast&lt;object&gt;().Count() 会遍历“当前视图”，
    /// 得到的是筛选后真正显示的行数。
    /// </summary>
    public string CountText => $"{AlarmView.Cast<object>().Count()} / {Alarms.Count} 条";

    public ICommand AddAlarmCommand { get; }

    /// <summary>按当前选择设置排序规则。可以添加多个排序字段（先按设备，再按时间）。</summary>
    private void ApplySort()
    {
        _alarmView.SortDescriptions.Clear();

        if (SelectedSort == "时间：最早在前")
        {
            _alarmView.SortDescriptions.Add(new SortDescription(nameof(AlarmRow.Time), ListSortDirection.Ascending));
        }
        else if (SelectedSort == "按设备名称")
        {
            _alarmView.SortDescriptions.Add(new SortDescription(nameof(AlarmRow.Device), ListSortDirection.Ascending));
            _alarmView.SortDescriptions.Add(new SortDescription(nameof(AlarmRow.Time), ListSortDirection.Descending));
        }
        else
        {
            // 默认：最新在前。
            _alarmView.SortDescriptions.Add(new SortDescription(nameof(AlarmRow.Time), ListSortDirection.Descending));
        }

        _alarmView.Refresh();
    }

    /// <summary>视图的过滤规则：严重程度必须匹配，且关键字必须能在三个字段之一里找到。</summary>
    private bool FilterAlarm(object item)
    {
        if (item is not AlarmRow row) return false;

        // 条件 1：严重程度。
        if (SelectedSeverity != "全部" && row.Severity != SelectedSeverity)
            return false;

        // 搜索框为空：不再限制关键字。
        if (string.IsNullOrWhiteSpace(SearchText))
            return true;

        // 条件 2：关键字匹配设备名、位号或说明。
        // OrdinalIgnoreCase 表示忽略大小写，适合英文/数字的搜索。
        var query = SearchText.Trim();
        return row.Device.Contains(query, StringComparison.OrdinalIgnoreCase)
               || row.Tag.Contains(query, StringComparison.OrdinalIgnoreCase)
               || row.Message.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>生成 24 条初始报警，覆盖三种严重程度和多种设备。</summary>
    private void SeedAlarms()
    {
        var now = DateTime.Now;
        for (var i = 0; i < 24; i++)
        {
            // 余数 0/4 报警，1/5 故障，其余提示。
            var severity = (i % 6) switch
            {
                0 or 4 => "报警",
                1 or 5 => "故障",
                _ => "提示"
            };
            var device = Devices[i % Devices.Length];
            var tag = Tags[(i * 2 + 1) % Tags.Length];
            var value = Math.Round(30 + _random.NextDouble() * 110, 2);

            Alarms.Add(new AlarmRow
            {
                Time = now.AddMinutes(-i * 7 - _random.Next(0, 5)),
                Device = device,
                Tag = tag,
                Severity = severity,
                Value = value,
                Message = severity switch
                {
                    "报警" => $"{tag}超过高高限",
                    "故障" => "设备通信超时，数据质量 Bad",
                    _ => "参数接近报警值，请关注"
                },
                Acknowledged = i % 4 == 0
            });
        }
    }

    /// <summary>按钮“新增模拟报警”：插入一条当前时间的数据，验证视图会自动更新。</summary>
    private void AddSampleAlarm()
    {
        var severity = _random.Next(0, 3) switch { 0 => "报警", 1 => "故障", _ => "提示" };
        var tag = Tags[_random.Next(Tags.Length)];

        // 插入 ObservableCollection 后，它的 CollectionChanged 会通知视图；
        // 视图再按当前 Filter/Sort 决定这条新数据排在哪里。
        Alarms.Insert(0, new AlarmRow
        {
            Time = DateTime.Now,
            Device = Devices[_random.Next(Devices.Length)],
            Tag = tag,
            Severity = severity,
            Value = Math.Round(40 + _random.NextDouble() * 90, 2),
            Message = $"模拟新增：{tag}触发{severity}",
            Acknowledged = false
        });

        OnPropertyChanged(nameof(CountText));
    }
}
