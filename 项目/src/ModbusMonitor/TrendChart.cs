using System.Drawing.Drawing2D;

namespace ModbusMonitor;

/// <summary>
/// 手写趋势曲线控件（GDI+ 双缓冲）。
///
/// 上位机项目里图表库很多（LiveCharts、ScottPlot、DevExpress Chart），
/// 但“自己画一遍”能彻底搞懂：坐标变换、量程自适应、报警带、双缓冲防闪烁。
/// 生产项目可以直接用库，这里保留下来是为了教学与排障（客户现场经常不允许装第三方库）。
/// </summary>
public sealed class TrendChart : Control
{
    // 最多保留 1500 个点，超出的从头部删掉，防止内存无限增长
    private const int MaxPoints = 1500;

    // 数据点列表；采集线程会 Add，UI 线程会绘制，所以读写都要加锁
    private readonly List<(DateTime Time, double Value)> _points = new();
    private readonly object _sync = new();

    public TrendChart()
    {
        // 四个样式的作用：
        // UserPaint           = 自己画，不交给系统
        // AllPaintingInWmPaint= 只在 WM_PAINT 里画，减少闪烁
        // OptimizedDoubleBuffer = 双缓冲：先画到内存位图，再一次性贴到屏幕
        // ResizeRedraw        = 窗口大小变化时重画
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Color.FromArgb(18, 20, 24);
        ForeColor = Color.Gainsboro;
        Font = new Font("Microsoft YaHei UI", 9F);
    }

    public string Title { get; set; } = "未选择位号";
    public string Unit { get; set; } = "";
    public double? LowLimit { get; set; }
    public double? HighLimit { get; set; }

    /// <summary>追加一个数据点并请求重绘（可在任意线程调用）。</summary>
    public void AddPoint(DateTime time, double value)
    {
        lock (_sync)
        {
            _points.Add((time, value));
            if (_points.Count > MaxPoints)
                _points.RemoveRange(0, _points.Count - MaxPoints);
        }
        Invalidate(); // 告诉系统“我需要重画”，系统会择机调用 OnPaint
    }

    /// <summary>切换位号时清空曲线，并更新标题、单位、报警限。</summary>
    public void Reset(string title, string unit, double? low, double? high)
    {
        lock (_sync) _points.Clear();
        Title = title;
        Unit = unit;
        LowLimit = low;
        HighLimit = high;
        Invalidate();
    }

    /// <summary>
    /// 绘制入口。分成五步：清屏 → 画坐标框 → 算量程 → 画网格和刻度 → 画曲线和提示。
    /// 所有 GDI+ 对象（Font、Brush、Pen）都用 using，用完立即释放，否则长时间运行会泄漏句柄。
    /// </summary>
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias; // 抗锯齿，曲线更平滑
        g.Clear(BackColor);

        using var titleFont = new Font(Font.FontFamily, 11F, FontStyle.Bold);
        using var smallFont = new Font(Font.FontFamily, 8.5F);
        using var textBrush = new SolidBrush(Color.Gainsboro);
        using var dimBrush = new SolidBrush(Color.FromArgb(140, 150, 160));
        using var gridPen = new Pen(Color.FromArgb(44, 48, 56), 1F);
        using var axisPen = new Pen(Color.FromArgb(80, 86, 96), 1F);
        using var linePen = new Pen(Color.FromArgb(0, 220, 170), 1.8F);

        g.DrawString(Title, titleFont, textBrush, 12, 6);

        // 绘图区：左边留 62px 写 Y 轴刻度，底部留 28px 写时间
        var plot = new Rectangle(62, 34, Math.Max(10, Width - 62 - 16), Math.Max(10, Height - 34 - 28));
        g.DrawRectangle(axisPen, plot);

        // 拷一份快照再画，避免绘制过程中采集线程修改集合导致异常
        List<(DateTime Time, double Value)> snapshot;
        lock (_sync) snapshot = _points.ToList();

        if (snapshot.Count == 0)
        {
            g.DrawString("等待数据…（在左侧表格选中一个位号）", smallFont, dimBrush, plot.Left + 16, plot.Top + 16);
            return;
        }

        // 量程自适应：取数据最小/最大值，并保证报警线也在可视范围内
        var min = snapshot.Min(p => p.Value);
        var max = snapshot.Max(p => p.Value);
        if (HighLimit is { } high) max = Math.Max(max, high);
        if (LowLimit is { } low) min = Math.Min(min, low);

        // 所有值都一样时，上下各扩 1，避免除以 0
        if (Math.Abs(max - min) < 1e-9) { max += 1; min -= 1; }
        var padding = (max - min) * 0.12; // 上下留 12% 余量，曲线不贴边
        min -= padding;
        max += padding;

        // 横向网格 + Y 轴刻度（5 等分）
        for (var i = 0; i <= 5; i++)
        {
            var y = plot.Bottom - plot.Height * i / 5f;
            g.DrawLine(gridPen, plot.Left, y, plot.Right, y);
            var value = min + (max - min) * i / 5.0;
            g.DrawString(value.ToString("0.###"), smallFont, dimBrush, 4, y - 8);
        }

        // X 轴时间刻度（首/中/尾）
        for (var i = 0; i <= 4; i++)
        {
            var x = plot.Left + plot.Width * i / 4f;
            g.DrawLine(gridPen, x, plot.Top, x, plot.Bottom);

            // 按比例找到对应数据点的时间
            var index = (int)Math.Round((snapshot.Count - 1) * i / 4.0);
            var label = snapshot[Math.Clamp(index, 0, snapshot.Count - 1)].Time.ToString("HH:mm:ss");
            g.DrawString(label, smallFont, dimBrush, x - 20, plot.Bottom + 4);
        }

        // 报警上下限带：虚线 + 文字
        if (HighLimit is { } hi)
        {
            var y = MapY(hi, plot, min, max);
            using var dash = new Pen(Color.FromArgb(230, 80, 80), 1F) { DashStyle = DashStyle.Dash };
            g.DrawLine(dash, plot.Left, y, plot.Right, y);
            g.DrawString($"上限 {hi:0.###}", smallFont, Brushes.Salmon, plot.Right - 70, y - 14);
        }
        if (LowLimit is { } lo)
        {
            var y = MapY(lo, plot, min, max);
            using var dash = new Pen(Color.FromArgb(90, 160, 240), 1F) { DashStyle = DashStyle.Dash };
            g.DrawLine(dash, plot.Left, y, plot.Right, y);
            g.DrawString($"下限 {lo:0.###}", smallFont, Brushes.LightSkyBlue, plot.Right - 70, y + 2);
        }

        // 曲线：把每个点换算成屏幕坐标，然后连成折线
        var points = new PointF[snapshot.Count];
        for (var i = 0; i < snapshot.Count; i++)
        {
            // X 按索引均匀分布；只有一个点时放中间
            var x = snapshot.Count == 1 ? plot.Left + plot.Width / 2f : plot.Left + plot.Width * i / (snapshot.Count - 1f);
            points[i] = new PointF(x, MapY(snapshot[i].Value, plot, min, max));
        }

        if (points.Length >= 2)
        {
            g.SetClip(plot); // 裁剪，防止曲线画出坐标框
            g.DrawLines(linePen, points);
            g.ResetClip();
        }
        else
        {
            // 只有一个点就画个小圆点
            g.FillEllipse(Brushes.Cyan, points[0].X - 2, points[0].Y - 2, 4, 4);
        }

        // 右上角显示最新值 + 单位
        var last = snapshot[^1];
        using var valueFont = new Font(Font.FontFamily, 16F, FontStyle.Bold);
        g.DrawString($"{last.Value:0.###}", valueFont, Brushes.White, plot.Right - 120, 8);
        g.DrawString(Unit, smallFont, dimBrush, plot.Right - 120 + g.MeasureString($"{last.Value:0.###}", valueFont).Width, 20);
    }

    /// <summary>
    /// 把数据值映射成屏幕 Y 坐标。
    /// 注意屏幕坐标 Y 轴朝下：值越大，Y 越小（越靠上），所以用 plot.Bottom 减去比例高度。
    /// </summary>
    private static float MapY(double value, Rectangle plot, double min, double max)
        => (float)(plot.Bottom - (value - min) / (max - min) * plot.Height);
}
