using System.ComponentModel;
using Exercises.WinForms.Controls;

namespace Exercises.WinForms.Views;

/// <summary>
/// 第 04 章练习 2：表格按报警状态筛选。
///
/// 题目：工具栏切换“全部 / 仅报警 / 仅故障 / 正常与恢复”，表格只显示匹配行；
/// 勾选自动模拟后，每 1.5 秒新增一条随机数据，筛选结果自动跟随。
///
/// 核心思路：
/// - _master 是“全量数据”，永远不因为筛选而删数据；
/// - ApplyFilter 根据当前条件，从 _master 里挑出子集，重新绑定给表格；
/// - 这样从“仅报警”切回“全部”时，其他数据还在。
///
/// 真实项目里如果表格用 BindingSource/DataView，可以直接设 Filter 字符串，
/// 不必每次重建绑定；本练习为了看清“筛选 = 选子集”的本质，用了重新绑定。
/// </summary>
internal sealed class AlarmFilterPanel : UserControl
{
    // 全量数据：所有报警记录都保存在这里。
    private readonly List<AlarmRow> _master = new();

    private readonly BufferedDataGridView _grid;
    private readonly Label _summary;

    // WinForms 的 Timer 会在 UI 线程触发 Tick，因此可以直接改控件，不需要 Invoke。
    private readonly System.Windows.Forms.Timer _autoTimer;
    private readonly Random _random = new();

    private string _filter = "全部";
    private int _sequence;   // 自动模拟数据的编号

    public AlarmFilterPanel()
    {
        var page = UiTheme.CreatePage(
            "第 04 章练习 2 · 按报警状态筛选",
            "工具栏切换“全部 / 仅报警 / 仅故障 / 正常与恢复”，表格只显示匹配行；勾选自动模拟后每 1.5 秒新增一条随机数据，筛选会自动跟随。",
            out var content);
        Dock = DockStyle.Fill;
        Controls.Add(page);

        // 三行：工具栏、表格、底部统计。
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = UiTheme.Surface
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // ---------- 筛选按钮 ----------
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true,
            Margin = new Padding(0, 0, 0, 10)
        };
        toolbar.Controls.Add(FilterButton("全部"));
        toolbar.Controls.Add(FilterButton("仅报警"));
        toolbar.Controls.Add(FilterButton("仅故障"));
        toolbar.Controls.Add(FilterButton("正常与恢复"));

        // ---------- 自动模拟 ----------
        _autoTimer = new System.Windows.Forms.Timer { Interval = 1500 };   // 1.5 秒
        _autoTimer.Tick += (_, _) => AddRandomRow();

        var auto = new CheckBox
        {
            Text = "自动模拟（1.5s）",
            AutoSize = true,
            Font = UiTheme.BodyFont,
            ForeColor = UiTheme.Text,
            Margin = new Padding(14, 8, 0, 0)
        };
        auto.CheckedChanged += (_, _) =>
        {
            if (auto.Checked) _autoTimer.Start();
            else _autoTimer.Stop();
        };
        toolbar.Controls.Add(auto);
        layout.Controls.Add(toolbar, 0, 0);

        // ---------- 表格 ----------
        _grid = new BufferedDataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AutoGenerateColumns = false,   // 手动定义列，不按属性自动生成
            AllowUserToAddRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            Margin = new Padding(0)
        };
        UiTheme.StyleGrid(_grid);

        // DataPropertyName 要和 AlarmRow 的属性名一致，表格才能自动取值。
        _grid.Columns.Add(Column("时间", "Time", 150));
        _grid.Columns.Add(Column("设备", "Device", 110));
        _grid.Columns.Add(Column("位号", "Tag", 150));
        var valueColumn = Column("数值", "Value", 90);
        valueColumn.DefaultCellStyle.Format = "F2";   // 数值显示两位小数
        _grid.Columns.Add(valueColumn);
        _grid.Columns.Add(Column("状态", "Status", 90));
        _grid.Columns.Add(Column("说明", "Message", 260));

        // CellFormatting 在“单元格准备显示”时触发，用来按状态改颜色。
        _grid.CellFormatting += Grid_CellFormatting;
        layout.Controls.Add(_grid, 0, 1);

        // ---------- 底部统计 ----------
        var footer = new TableLayoutPanel
        {
            ColumnCount = 1,
            RowCount = 2,
            Dock = DockStyle.Fill,
            AutoSize = true,
            BackColor = UiTheme.Surface,
            Margin = new Padding(0, 10, 0, 0)
        };
        _summary = UiTheme.Label("", color: UiTheme.Muted);
        footer.Controls.Add(_summary, 0, 0);
        footer.Controls.Add(UiTheme.Hint("筛选只改变表格显示的数据，不修改原始 List；如果表格启用 DataSource，可改用 BindingSource/DataView 的 Filter，" +
                                         "避免每次重建绑定。"), 0, 1);
        layout.Controls.Add(footer, 0, 2);
        content.Controls.Add(layout);

        SeedData();       // 造 12 条初始数据
        ApplyFilter();    // 按“全部”显示
    }

    /// <summary>创建一个筛选按钮。按钮的 Tag 里存筛选条件，点击时读取。</summary>
    private Button FilterButton(string text)
    {
        var button = UiTheme.Button(text);
        button.Tag = text;
        button.Click += (_, _) =>
        {
            _filter = text;
            ApplyFilter();
        };
        return button;
    }

    /// <summary>创建表格文本列：表头、绑定的属性名、列宽权重。</summary>
    private static DataGridViewTextBoxColumn Column(string header, string property, int fillWeight) => new()
    {
        HeaderText = header,
        DataPropertyName = property,
        FillWeight = fillWeight,
        SortMode = DataGridViewColumnSortMode.NotSortable
    };

    /// <summary>生成 12 条初始报警记录，覆盖报警/故障/已恢复/正常四种状态。</summary>
    private void SeedData()
    {
        var now = DateTime.Now;
        var tags = new[] { "炉膛温度", "出口压力", "电机电流", "冷却水温度", "主轴转速" };

        for (var i = 0; i < 12; i++)
        {
            // i % 5 的余数决定状态，保证四种状态都能出现。
            var status = (i % 5) switch
            {
                0 => "报警",
                1 => "故障",
                2 => "已恢复",
                _ => "正常"
            };

            _master.Add(new AlarmRow(
                now.AddSeconds(-i * 37),                       // 时间依次往前推，模拟历史记录
                i % 2 == 0 ? "1#炉" : "2#炉",
                tags[i % tags.Length],
                60 + _random.NextDouble() * 60,                // 60~120 的随机数
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

    /// <summary>自动模拟：插入一条新记录到最前面，并按其状态参与当前筛选。</summary>
    private void AddRandomRow()
    {
        _sequence++;
        var tags = new[] { "炉膛温度", "出口压力", "电机电流", "冷却水温度", "主轴转速" };

        // Next(0, 10) 得到 0~9，再用区间决定状态，概率大致可控。
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
            tags[_random.Next(tags.Length)],
            60 + _random.NextDouble() * 60,
            status,
            $"模拟数据 #{_sequence}"));

        // 防止无限增长：超过 200 条就删最旧的一条。
        if (_master.Count > 200) _master.RemoveAt(_master.Count - 1);

        ApplyFilter();
    }

    /// <summary>按当前 _filter 从全量数据中挑出要显示的子集，重新绑定表格。</summary>
    private void ApplyFilter()
    {
        // 先让 query 指向全量数据，再根据条件逐层收窄。
        IEnumerable<AlarmRow> query = _master;
        query = _filter switch
        {
            "仅报警" => query.Where(r => r.Status == "报警"),
            "仅故障" => query.Where(r => r.Status == "故障"),
            "正常与恢复" => query.Where(r => r.Status is "正常" or "已恢复"),
            _ => query   // “全部”不做筛选
        };

        // ToList 真正执行查询，得到当前要显示的行。
        var visible = query.ToList();

        // BindingList 适合直接作为 DataGridView 的数据源，增删改会通知表格刷新。
        // 这里每次都新建一个，数据量不大；真实项目可复用 BindingSource。
        _grid.DataSource = new BindingList<AlarmRow>(visible);
        _summary.Text = $"当前筛选：{_filter} · 共 {_master.Count} 条，显示 {visible.Count} 条";
    }

    /// <summary>按“状态”列给整行上色，让报警/故障更醒目。</summary>
    private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.CellStyle is null) return;

        // DataBoundItem 是该行绑定的原始对象，转回 AlarmRow 才能读状态。
        if (_grid.Rows[e.RowIndex].DataBoundItem is not AlarmRow row) return;

        switch (row.Status)
        {
            case "报警":
                e.CellStyle.BackColor = UiTheme.AlarmBg;
                e.CellStyle.ForeColor = UiTheme.Alarm;
                break;
            case "故障":
                e.CellStyle.BackColor = Color.FromArgb(245, 247, 250);
                e.CellStyle.ForeColor = UiTheme.Muted;
                break;
            case "已恢复":
                e.CellStyle.ForeColor = Color.FromArgb(21, 128, 61);   // 绿色
                break;
            default:
                e.CellStyle.ForeColor = UiTheme.Text;
                break;
        }
    }

    /// <summary>表格的一行数据。record 是“只装数据”的轻量类，编译器自动生成构造和属性。</summary>
    private sealed record AlarmRow(DateTime Time, string Device, string Tag, double Value, string Status, string Message);
}
