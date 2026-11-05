using System.ComponentModel;
using Answers.WinForms.Models;

namespace Answers.WinForms.Forms;

/// <summary>
/// 第 04 章练习 2：表格按报警状态筛选。
///
/// 题目：工具栏切换“全部 / 仅报警 / 仅故障 / 正常与恢复”，
/// 表格只显示匹配的行；勾选“自动模拟”后每 1.5 秒新增一条随机数据，
/// 当前筛选结果自动跟随。
///
/// 核心思路：
/// - _master 保存“全量数据”，筛选时一条都不删；
/// - ApplyFilter 从 _master 里挑出一个子集，重新绑定给表格；
/// - 所以从“仅报警”切回“全部”时，其它数据还完整存在。
///
/// 设计器说明：
/// 界面上的按钮、表格、列的标题全部写在 AlarmFilterForm.Designer.cs，
/// 本文件只写“用户点击之后做什么”。
/// </summary>
public partial class AlarmFilterForm : Form
{
    private readonly List<AlarmRow> _master = new();
    private readonly Random _random = new();

    private string _filter = "全部";
    private int _sequence;

    private static readonly string[] Tags =
    {
        "炉膛温度", "出口压力", "电机电流", "冷却水温度", "主轴转速"
    };

    public AlarmFilterForm()
    {
        InitializeComponent();

        // 设计器已经设置了大体样式，这里再统一补齐表头、行高、隔行变色等细节。
        UiTheme.StyleGrid(grid);

        SeedData();
        ApplyFilter();
        UpdateFilterButtons();
    }

    // ------------------------------------------------------------------
    // 数据部分
    // ------------------------------------------------------------------

    /// <summary>生成 12 条初始记录，让四种状态都能看到。</summary>
    private void SeedData()
    {
        var now = DateTime.Now;

        for (var i = 0; i < 12; i++)
        {
            // i % 5 的余数决定状态，保证“报警/故障/已恢复/正常”都会出现。
            var status = (i % 5) switch
            {
                0 => "报警",
                1 => "故障",
                2 => "已恢复",
                _ => "正常"
            };

            _master.Add(new AlarmRow(
                now.AddSeconds(-i * 37),
                i % 2 == 0 ? "1#炉" : "2#炉",
                Tags[i % Tags.Length],
                60 + _random.NextDouble() * 60,
                status,
                status switch
                {
                    "报警" => "超过高限，等待确认",
                    "故障" => "通信超时，质量为 Bad",
                    "已恢复" => "已回到正常范围",
                    _ => "运行正常"
                }));
        }
    }

    /// <summary>自动模拟：在列表最前面插入一条新记录，再按当前条件筛选。</summary>
    private void AddRandomRow()
    {
        _sequence++;

        var status = _random.Next(0, 10) switch
        {
            <= 2 => "报警",
            <= 4 => "故障",
            <= 5 => "已恢复",
            _ => "正常"
        };

        _master.Insert(0, new AlarmRow(
            DateTime.Now,
            _random.Next(2) == 0 ? "1#炉" : "2#炉",
            Tags[_random.Next(Tags.Length)],
            60 + _random.NextDouble() * 60,
            status,
            $"模拟数据 #{_sequence}"));

        // 防止无限增长：超过 200 条就删掉最旧的一条。
        if (_master.Count > 200)
            _master.RemoveAt(_master.Count - 1);

        ApplyFilter();
    }

    /// <summary>按当前筛选条件挑出子集，重新绑定到表格。</summary>
    private void ApplyFilter()
    {
        IEnumerable<AlarmRow> query = _master;
        query = _filter switch
        {
            "仅报警" => query.Where(r => r.Status == "报警"),
            "仅故障" => query.Where(r => r.Status == "故障"),
            "正常与恢复" => query.Where(r => r.Status is "正常" or "已恢复"),
            _ => query
        };

        var visible = query.ToList();

        // BindingList 作为 DataSource 时，表格会随集合变化自动刷新。
        // 这里数据量很小，每次重建绑定最直观；真实项目可复用 BindingSource。
        grid.DataSource = new BindingList<AlarmRow>(visible);

        summaryLabel.Text =
            $"当前筛选：{_filter} · 全量 {_master.Count} 条，显示 {visible.Count} 条";
    }

    // ------------------------------------------------------------------
    // 界面事件
    // ------------------------------------------------------------------

    /// <summary>四个筛选按钮共用这一个事件，按钮的文字就是筛选条件。</summary>
    private void FilterButton_Click(object? sender, EventArgs e)
    {
        if (sender is not Button button) return;

        _filter = button.Text;
        ApplyFilter();
        UpdateFilterButtons();
    }

    /// <summary>把当前选中的筛选按钮染成强调色，让用户一眼看出处于哪种筛选。</summary>
    private void UpdateFilterButtons()
    {
        foreach (var button in new[] { btnAll, btnAlarmOnly, btnFaultOnly, btnRecovered })
        {
            var active = button.Text == _filter;
            button.BackColor = active ? UiTheme.Accent : UiTheme.Surface;
            button.ForeColor = active ? Color.White : UiTheme.Text;
        }
    }

    private void autoCheck_CheckedChanged(object? sender, EventArgs e)
    {
        if (autoCheck.Checked)
            autoTimer.Start();
        else
            autoTimer.Stop();
    }

    private void autoTimer_Tick(object? sender, EventArgs e) => AddRandomRow();

    /// <summary>
    /// CellFormatting 在“单元格即将显示”时触发。
    /// 这里只给可见单元格上色，不会遍历全部数据。
    /// </summary>
    private void grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.CellStyle is null) return;
        if (grid.Rows[e.RowIndex].DataBoundItem is not AlarmRow row) return;

        switch (row.Status)
        {
            case "报警":
                e.CellStyle.BackColor = UiTheme.AlarmBg;
                e.CellStyle.ForeColor = UiTheme.Alarm;
                break;
            case "故障":
                e.CellStyle.BackColor = UiTheme.SurfaceAlt;
                e.CellStyle.ForeColor = UiTheme.Muted;
                break;
            case "已恢复":
                e.CellStyle.ForeColor = UiTheme.Success;
                break;
            default:
                e.CellStyle.ForeColor = UiTheme.Text;
                break;
        }
    }
}
