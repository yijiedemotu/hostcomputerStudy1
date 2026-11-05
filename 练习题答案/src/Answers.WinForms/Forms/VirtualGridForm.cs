using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Answers.WinForms.Forms;

/// <summary>
/// 第 04 章练习 4：用 VirtualMode 加载 10 万行历史数据。
///
/// 普通写法的问题：
/// 给 DataGridView 一行行 Add 10 万个 DataGridViewRow，
/// 会创建 10 万个重量级对象，打开慢、占内存、滚动卡，界面甚至假死。
///
/// VirtualMode 的思路：
/// - 数据只放在一个 HistoryRow[] 数组里（结构体，内存连续）；
/// - 用 RowCount 告诉表格“总共有 10 万行”，但不真正创建行对象；
/// - 表格只在“某个单元格即将显示”时触发 CellValueNeeded，向你要这一格的值；
/// - 屏幕一次只显示十几行，所以滚动时每次只计算几十个单元格。
///
/// 这就是上位机查看海量历史数据的常用做法。
/// </summary>
public partial class VirtualGridForm : Form
{
    private const int RowCount = 100_000;

    private static readonly string[] PointNames =
    {
        "炉膛温度", "主蒸汽压力", "给水流量", "烟气含氧量", "引风机电流",
        "省煤器出口温度", "过热器出口温度", "除尘器差压", "循环泵频率", "补水流量"
    };

    private HistoryRow[] _rows = Array.Empty<HistoryRow>();
    private long _cellRequests;
    private long _lastRequests;

    public VirtualGridForm()
    {
        InitializeComponent();

        UiTheme.StyleGrid(grid);

        BuildRows();
        statsTimer.Start();
    }

    /// <summary>生成 10 万条历史记录，只放进数组，不创建 DataGridViewRow。</summary>
    private void BuildRows()
    {
        var stopwatch = Stopwatch.StartNew();
        var rows = new HistoryRow[RowCount];
        var start = DateTime.Today.AddHours(8);   // 从今天 08:00 开始，每行 +5 秒

        for (var i = 0; i < rows.Length; i++)
        {
            // 用正弦波造一条有起伏的曲线，再叠加一点周期性小偏移。
            var value = 50 + 30 * Math.Sin(i / 137.0) + i % 17 * 0.35;

            // 每 3433 行造一个“故障”，每 1711 行造一个“报警”，其余正常。
            // 取余运算让异常数据分布均匀、可重复。
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
        grid.RowCount = rows.Length;
        grid.ClearSelection();
        _cellRequests = 0;
        _lastRequests = 0;

        stopwatch.Stop();

        // Marshal.SizeOf 计算一个结构体占多少字节，用来估算数组总内存。
        var bytes = (long)rows.Length * Marshal.SizeOf<HistoryRow>();
        generationLabel.Text =
            $"生成 {RowCount:N0} 行：{stopwatch.ElapsedMilliseconds} ms · 结构体数组约 {bytes / 1024d / 1024d:F1} MB（没有 10 万个 DataGridViewRow）";

        UpdateVisibleRange();
    }

    /// <summary>跳到指定行并选中它。</summary>
    private void JumpTo(int rowIndex)
    {
        if (_rows.Length == 0) return;

        // Math.Clamp 把索引限制在合法范围内，防止越界。
        rowIndex = Math.Clamp(rowIndex, 0, _rows.Length - 1);

        // FirstDisplayedScrollingRowIndex 控制表格从哪一行开始显示。
        grid.FirstDisplayedScrollingRowIndex = rowIndex;
        grid.ClearSelection();
        grid.Rows[rowIndex].Selected = true;
        UpdateVisibleRange();
    }

    /// <summary>
    /// 虚拟模式的核心：表格问“第 e.RowIndex 行、第 e.ColumnIndex 列显示什么”，
    /// 我们现场从数组里算出来，塞进 e.Value。
    /// </summary>
    private void grid_CellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
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
    private void grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _rows.Length || e.CellStyle is null) return;

        var row = _rows[e.RowIndex];

        // 数值列在报警时标红。
        if (e.ColumnIndex == 3 && row.Status == 1)
            e.CellStyle.ForeColor = UiTheme.Alarm;

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
                e.CellStyle.ForeColor = UiTheme.Success;
                break;
        }
    }

    /// <summary>更新“当前可见第几行到第几行”的提示。</summary>
    private void UpdateVisibleRange()
    {
        if (_rows.Length == 0) return;

        var first = grid.FirstDisplayedScrollingRowIndex;
        if (first < 0) first = 0;

        // DisplayedRowCount 返回屏幕上能完整显示的行数，通常只有十几行。
        var visibleCount = Math.Max(1, grid.DisplayedRowCount(includePartialRow: false));
        var last = Math.Min(_rows.Length, first + visibleCount);

        visibleLabel.Text = $"当前可见第 {first + 1:N0}–{last:N0} 行 / 共 {_rows.Length:N0} 行";
    }

    // ------------------------------------------------------------------
    // 工具栏事件
    // ------------------------------------------------------------------

    private void btnReload_Click(object? sender, EventArgs e) => BuildRows();

    private void btnJump_Click(object? sender, EventArgs e) => JumpTo(50_000 - 1);

    private void btnBottom_Click(object? sender, EventArgs e) => JumpTo(_rows.Length - 1);

    private void btnTop_Click(object? sender, EventArgs e) => JumpTo(0);

    private void grid_Scroll(object? sender, ScrollEventArgs e) => UpdateVisibleRange();

    /// <summary>每秒统计一次单元格求值次数，验证“只请求可见行”。</summary>
    private void statsTimer_Tick(object? sender, EventArgs e)
    {
        var delta = _cellRequests - _lastRequests;
        _lastRequests = _cellRequests;

        requestLabel.Text =
            $"CellValueNeeded：最近 1 秒 {delta:N0} 次，累计 {_cellRequests:N0} 次 · 单元格只在你看到它时才会被求值";
        UpdateVisibleRange();
    }

    /// <summary>
    /// 一行历史数据。
    /// 用 readonly record struct（结构体）而不是 class：
    /// 10 万个对象分散在托管堆上，占用大、GC 压力高；
    /// 结构体数组是连续内存，更省空间、缓存更友好。
    /// </summary>
    private readonly record struct HistoryRow(DateTime Time, int PointIndex, double Value, byte Status);
}
