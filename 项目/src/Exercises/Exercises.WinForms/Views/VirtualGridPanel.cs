using System.Diagnostics;
using System.Runtime.InteropServices;
using Exercises.WinForms.Controls;

namespace Exercises.WinForms.Views;

/// <summary>
/// 第 04 章练习 4：用 VirtualMode 加载 10 万行历史数据。
///
/// 普通写法的问题：
/// 给 DataGridView 一行行 Add 10 万个 DataGridViewRow，会创建 10 万个重量级对象，
/// 打开慢、占内存、滚动卡，界面甚至直接假死。
///
/// VirtualMode 的思路：
/// - 数据只放在一个 HistoryRow[] 数组里（结构体，内存紧凑）；
/// - 告诉表格“总共有 10 万行”（RowCount），但不要真正创建行对象；
/// - 表格只会在“某个单元格即将显示”时触发 CellValueNeeded，向你要这一格的值；
/// - 屏幕一次只显示十几行，所以每次滚动只需要计算几十个单元格。
///
/// 这就是上位机查看海量历史数据的常用做法。
/// </summary>
internal sealed class VirtualGridPanel : UserControl
{
    private const int RowCount = 100_000;   // 10 万行

    // 演示用点位名称。真实项目从配置/数据库加载。
    private static readonly string[] PointNames =
    {
        "炉膛温度", "主蒸汽压力", "给水流量", "烟气含氧量", "引风机电流",
        "省煤器出口温度", "过热器出口温度", "除尘器差压", "循环泵频率", "补水流量"
    };

    private readonly BufferedDataGridView _grid;
    private readonly Label _generationLabel;   // 生成耗时/内存
    private readonly Label _visibleLabel;      // 当前可见行范围
    private readonly Label _requestLabel;      // CellValueNeeded 调用次数
    private readonly System.Windows.Forms.Timer _statsTimer;

    private HistoryRow[] _rows = Array.Empty<HistoryRow>();
    private long _cellRequests;
    private long _lastRequests;

    public VirtualGridPanel()
    {
        var page = UiTheme.CreatePage(
            "练习 04-4 · VirtualMode 加载 10 万行",
            "VirtualMode + CellValueNeeded：数据放结构体数组，表格只向你要“当前可见行”的单元格。滚动时留意底部计数，每次只请求屏幕上那十几行。",
            out var content);
        Dock = DockStyle.Fill;
        Controls.Add(page);

        // 三行：工具栏（46px）、表格（剩余）、统计信息（94px）。
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = UiTheme.Surface
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 94));

        // ---------- 工具栏 ----------
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            BackColor = UiTheme.Surface,
            Margin = new Padding(0),
            Padding = new Padding(0, 5, 0, 5)
        };
        var reload = UiTheme.Button("重新生成 10 万行", primary: true);
        reload.Click += (_, _) => BuildRows();
        toolbar.Controls.Add(reload);

        var jump = UiTheme.Button("跳到第 50,000 行");
        jump.Click += (_, _) => JumpTo(50_000 - 1);   // 界面上第 5 万行，索引是 49999
        toolbar.Controls.Add(jump);

        var bottom = UiTheme.Button("滚动到底部");
        bottom.Click += (_, _) => JumpTo(_rows.Length - 1);
        toolbar.Controls.Add(bottom);

        var top = UiTheme.Button("回到顶部");
        top.Click += (_, _) => JumpTo(0);
        toolbar.Controls.Add(top);
        layout.Controls.Add(toolbar, 0, 0);

        // ---------- 表格 ----------
        _grid = new BufferedDataGridView
        {
            Dock = DockStyle.Fill,
            VirtualMode = true,          // ← 关键：开启虚拟模式
            ReadOnly = true,
            AutoGenerateColumns = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            Margin = new Padding(0)
        };
        UiTheme.StyleGrid(_grid);
        _grid.Columns.Add(Column("序号", 60));
        _grid.Columns.Add(Column("采集时间", 180));
        _grid.Columns.Add(Column("点位名称", 160));
        _grid.Columns.Add(Column("数值", 100));
        _grid.Columns.Add(Column("状态", 90));
        _grid.Columns.Add(Column("备注", 260));

        // VirtualMode 下，表格不知道单元格内容，必须由这个事件提供。
        _grid.CellValueNeeded += Grid_CellValueNeeded;

        // 格式（颜色）仍可用 CellFormatting 统一设置。
        _grid.CellFormatting += Grid_CellFormatting;

        _grid.Scroll += (_, _) => UpdateVisibleRange();
        layout.Controls.Add(_grid, 0, 1);

        // ---------- 底部统计 ----------
        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = UiTheme.Surface,
            Margin = new Padding(0, 6, 0, 0)
        };
        _generationLabel = UiTheme.Label("", color: UiTheme.Text);
        _visibleLabel = UiTheme.Label("", color: UiTheme.Text);
        _requestLabel = UiTheme.Label("", color: UiTheme.Muted);
        footer.Controls.Add(_generationLabel, 0, 0);
        footer.Controls.Add(_visibleLabel, 0, 1);
        footer.Controls.Add(_requestLabel, 0, 2);
        layout.Controls.Add(footer, 0, 2);

        content.Controls.Add(layout);

        BuildRows();

        // 每秒统计一次：最近 1 秒单元格求值次数，验证“只请求可见行”。
        _statsTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _statsTimer.Tick += (_, _) =>
        {
            var delta = _cellRequests - _lastRequests;
            _lastRequests = _cellRequests;
            _requestLabel.Text = $"CellValueNeeded：最近 1 秒 {delta:N0} 次，累计 {_cellRequests:N0} 次 · 每个单元格只在你看到它时才格式化";
            UpdateVisibleRange();
        };
        _statsTimer.Start();
    }

    /// <summary>创建表格列；虚拟模式下只定义“列”，行数由 RowCount 决定。</summary>
    private static DataGridViewTextBoxColumn Column(string header, int fillWeight) => new()
    {
        HeaderText = header,
        FillWeight = fillWeight,
        SortMode = DataGridViewColumnSortMode.NotSortable
    };

    /// <summary>生成 10 万条历史记录，只放进数组，不创建 DataGridViewRow。</summary>
    private void BuildRows()
    {
        var stopwatch = Stopwatch.StartNew();
        var rows = new HistoryRow[RowCount];
        var start = DateTime.Today.AddHours(8);   // 从今天 08:00 开始，每行 +5 秒

        for (var i = 0; i < rows.Length; i++)
        {
            // 用正弦波造一条有起伏的曲线，再叠加一点周期性的小偏移。
            var value = 50 + 30 * Math.Sin(i / 137.0) + i % 17 * 0.35;

            // 每 3433 行造一个“故障”，每 1711 行造一个“报警”，其余正常。
            // 用取余运算是为了让异常数据分布均匀、可重复。
            var status = i % 3433 == 0 ? (byte)2
                : i % 1711 == 0 ? (byte)1
                : (byte)0;

            rows[i] = new HistoryRow(
                start.AddSeconds(i * 5L),
                i % PointNames.Length,
                Math.Round(value, 2),
                status);
        }

        _rows = rows;

        // 只告诉表格“有这么多行”，并不会真的创建 10 万个行控件。
        _grid.RowCount = rows.Length;
        _grid.ClearSelection();
        _cellRequests = 0;
        _lastRequests = 0;

        stopwatch.Stop();

        // Marshal.SizeOf 计算一个结构体占多少字节，用来估算数组总内存。
        var bytes = (long)rows.Length * Marshal.SizeOf<HistoryRow>();
        _generationLabel.Text = $"生成 {RowCount:N0} 行：{stopwatch.ElapsedMilliseconds} ms · 结构体数组约 {bytes / 1024d / 1024d:F1} MB（没有 10 万个 DataGridViewRow）";
        UpdateVisibleRange();
    }

    /// <summary>跳到指定行，并选中它。</summary>
    private void JumpTo(int rowIndex)
    {
        if (_rows.Length == 0) return;

        // Math.Clamp 把索引限制在合法范围内，防止越界。
        rowIndex = Math.Clamp(rowIndex, 0, _rows.Length - 1);

        // FirstDisplayedScrollingRowIndex 控制表格从哪一行开始显示。
        _grid.FirstDisplayedScrollingRowIndex = rowIndex;
        _grid.ClearSelection();
        _grid.Rows[rowIndex].Selected = true;
        UpdateVisibleRange();
    }

    /// <summary>
    /// 虚拟模式的核心：表格问“第 e.RowIndex 行、第 e.ColumnIndex 列显示什么”，
    /// 我们现场从数组里算出来，塞进 e.Value。
    /// </summary>
    private void Grid_CellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _rows.Length) return;

        _cellRequests++;   // 统计被请求的单元格数量

        var row = _rows[e.RowIndex];
        e.Value = e.ColumnIndex switch
        {
            0 => e.RowIndex + 1,                             // 序号从 1 开始
            1 => row.Time.ToString("yyyy-MM-dd HH:mm:ss"),
            2 => PointNames[row.PointIndex],
            3 => row.Value.ToString("F2"),
            4 => row.Status switch { 1 => "报警", 2 => "故障", _ => "正常" },
            5 => row.Status switch
            {
                1 => "超过高高限，等待确认",
                2 => "通信超时，数据质量 Bad",
                _ => "OK"
            },
            _ => null
        };
    }

    /// <summary>只为“可见单元格”设置颜色，不会遍历 10 万行。</summary>
    private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _rows.Length || e.CellStyle is null) return;
        var row = _rows[e.RowIndex];

        // 数值列在报警时标红。
        if (e.ColumnIndex == 3 && row.Status == 1)
            e.CellStyle.ForeColor = UiTheme.Alarm;

        // 状态列按状态整格上色。
        if (e.ColumnIndex != 4) return;
        switch (row.Status)
        {
            case 1:
                e.CellStyle.ForeColor = UiTheme.Alarm;
                e.CellStyle.BackColor = UiTheme.AlarmBg;
                break;
            case 2:
                e.CellStyle.ForeColor = UiTheme.Muted;
                e.CellStyle.BackColor = Color.FromArgb(241, 245, 249);
                break;
            default:
                e.CellStyle.ForeColor = Color.FromArgb(21, 128, 61);
                break;
        }
    }

    /// <summary>更新“当前可见第几行到第几行”的提示。</summary>
    private void UpdateVisibleRange()
    {
        if (_rows.Length == 0) return;

        var first = _grid.FirstDisplayedScrollingRowIndex;
        if (first < 0) first = 0;

        // DisplayedRowCount 返回屏幕上能完整显示的行数，通常只有十几行。
        var visibleCount = Math.Max(1, _grid.DisplayedRowCount(includePartialRow: false));
        var last = Math.Min(_rows.Length, first + visibleCount);

        _visibleLabel.Text = $"当前可见第 {first + 1:N0}–{last:N0} 行 / 共 {_rows.Length:N0} 行";
    }

    /// <summary>销毁时停掉统计定时器，避免后台继续触发。</summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _statsTimer.Stop();
            _statsTimer.Dispose();
        }
        base.Dispose(disposing);
    }

    /// <summary>
    /// 一行历史数据。
    /// 用 readonly record struct（结构体）而不是 class：
    /// 10 万个对象在托管堆上分散存放，占用大、GC 压力高；
    /// 结构体数组是连续内存，更省空间、缓存更友好。
    /// </summary>
    private readonly record struct HistoryRow(DateTime Time, int PointIndex, double Value, byte Status);
}
