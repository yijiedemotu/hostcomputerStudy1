using System.Drawing.Drawing2D;

namespace Answers.WinForms.Controls;

/// <summary>
/// 双缓冲自绘趋势图 + 鼠标悬停十字光标（第 04 章练习 3 的核心控件）。
///
/// 这个控件不依赖任何图表库，全部用 GDI+ 的 Graphics 手画：
/// 1. OnPaint 里依次画背景、坐标轴网格、半透明面积、折线、最新点；
/// 2. 鼠标移动时，把“像素 X 坐标”反算成“第几个数据点”；
/// 3. 再在该点画十字虚线和“时间 + 数值”提示框。
///
/// 坐标系提醒：
/// 屏幕坐标原点在左上角，Y 向下增大；
/// 而温度值是“越大越靠上”，所以 YAt 里要用 plot.Bottom 减去比例。
///
/// 它作为普通自定义控件放在设计器里（TrendHoverForm.Designer.cs），
/// 设计时显示为一块空白面板，运行时才由 OnPaint 画出曲线。
/// </summary>
public sealed class HoverTrendChart : Control
{
    private readonly List<TrendPoint> _points = new();
    private readonly int _maxPoints;
    private readonly Font _axisFont = new("Microsoft YaHei UI", 8.5F);
    private readonly Font _tipFont = new("Microsoft YaHei UI", 9F, FontStyle.Bold);

    // 当前鼠标悬停的数据点下标；-1 表示没有悬停。
    private int _hoverIndex = -1;

    public HoverTrendChart()
        : this(120)
    {
    }

    public HoverTrendChart(int maxPoints)
    {
        _maxPoints = maxPoints;

        // 自绘控件的四个关键开关：
        // AllPaintingInWmPaint：所有绘制都在 OnPaint 完成，减少闪烁；
        // OptimizedDoubleBuffer：先画在内存位图，再一次性贴到屏幕；
        // UserPaint：由本控件自己画，不用系统的默认绘制；
        // ResizeRedraw：窗口大小变化时自动重绘。
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint |
                 ControlStyles.ResizeRedraw, true);

        BackColor = UiTheme.Surface;
        Cursor = Cursors.Cross;
    }

    /// <summary>追加一个数据点；超过上限时丢掉最旧的，形成滚动窗口。</summary>
    public void AddPoint(DateTime time, double value)
    {
        _points.Add(new TrendPoint(time, value));

        if (_points.Count > _maxPoints)
        {
            _points.RemoveRange(0, _points.Count - _maxPoints);
            _hoverIndex = -1;   // 删除后下标变了，清除悬停状态
        }

        // Invalidate 只是“登记需要重画”，真正的绘制稍后走 OnPaint。
        Invalidate();
    }

    public void ClearPoints()
    {
        _points.Clear();
        _hoverIndex = -1;
        Invalidate();
    }

    /// <summary>
    /// 鼠标在图表上移动：把像素位置换算成最近的数据点。
    ///
    /// 1. 鼠标 X 相对绘图区左边的距离，除以绘图区宽度，得到 0~1 的比例；
    /// 2. 比例乘以 (点数 - 1)，四舍五入，就是最近点的下标。
    /// </summary>
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        var plot = PlotRect();

        if (_points.Count == 0 || !plot.Contains(e.Location))
        {
            if (_hoverIndex != -1)
            {
                _hoverIndex = -1;
                Invalidate();
            }
            return;
        }

        var ratio = Math.Clamp((e.X - plot.Left) / plot.Width, 0f, 1f);
        var index = (int)Math.Round(ratio * (_points.Count - 1));
        index = Math.Clamp(index, 0, _points.Count - 1);

        if (index == _hoverIndex) return;
        _hoverIndex = index;
        Invalidate();
    }

    /// <summary>鼠标移出控件：清除十字线和提示框。</summary>
    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_hoverIndex == -1) return;
        _hoverIndex = -1;
        Invalidate();
    }

    /// <summary>
    /// 绘制入口。系统需要重画时会调用它：
    /// 第一次显示、窗口移动、控件调用 Invalidate 之后等。
    /// 绘制顺序就是图层顺序，后画的会盖住先画的。
    /// </summary>
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;

        // AntiAlias 让曲线和圆点边缘平滑；ClearTypeGridFit 让文字清晰。
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        using var surface = new SolidBrush(BackColor);
        g.FillRectangle(surface, ClientRectangle);

        var plot = PlotRect();
        if (plot.Width < 20 || plot.Height < 20) return;

        using var plotBg = new SolidBrush(Color.FromArgb(250, 251, 253));
        g.FillRectangle(plotBg, plot);
        using var borderPen = new Pen(UiTheme.Border);
        g.DrawRectangle(borderPen, plot.X, plot.Y, plot.Width, plot.Height);

        if (_points.Count == 0)
        {
            using var empty = new SolidBrush(UiTheme.Muted);
            const string text = "等待数据…";
            var size = g.MeasureString(text, UiTheme.BodyFont);
            g.DrawString(text, UiTheme.BodyFont, empty,
                plot.Left + (plot.Width - size.Width) / 2,
                plot.Top + (plot.Height - size.Height) / 2);
            return;
        }

        // ---------- 计算 Y 轴范围 ----------
        var min = _points.Min(p => p.Value);
        var max = _points.Max(p => p.Value);

        // 所有值都一样时 max - min = 0，后面会除零；手动撑开 2 个单位。
        if (Math.Abs(max - min) < 0.001)
        {
            max += 1;
            min -= 1;
        }

        // 上下各留 12% 余量，避免曲线贴着边框。
        var padding = (max - min) * 0.12;
        min -= padding;
        max += padding;

        // ---------- 横向网格 + Y 轴刻度 ----------
        using var gridPen = new Pen(Color.FromArgb(232, 236, 242));
        using var axisBrush = new SolidBrush(UiTheme.Muted);
        using var labelFormat = new StringFormat
        {
            Alignment = StringAlignment.Far,
            LineAlignment = StringAlignment.Center
        };

        for (var i = 0; i <= 4; i++)
        {
            var y = plot.Top + plot.Height * i / 4f;
            g.DrawLine(gridPen, plot.Left, y, plot.Right, y);

            // 最上面是最大值，最下面是最小值，所以用 max 减去比例。
            var value = max - (max - min) * i / 4;
            g.DrawString(value.ToString("F1"), _axisFont, axisBrush,
                plot.Left - 8, y, labelFormat);
        }

        // ---------- X 轴时间刻度：起点、1/3、2/3、终点 ----------
        using var timeFormat = new StringFormat { Alignment = StringAlignment.Center };
        var labelIndices = new[] { 0, _points.Count / 3, _points.Count * 2 / 3, _points.Count - 1 }
            .Distinct()
            .ToArray();

        foreach (var index in labelIndices)
        {
            var x = XAt(index, plot);
            timeFormat.Alignment = index == 0 ? StringAlignment.Near
                : index == _points.Count - 1 ? StringAlignment.Far
                : StringAlignment.Center;

            g.DrawString(_points[index].Time.ToString("HH:mm:ss"), _axisFont, axisBrush,
                x, plot.Bottom + 7, timeFormat);
        }

        // ---------- 数据点 → 屏幕坐标 ----------
        var linePoints = new PointF[_points.Count];
        for (var i = 0; i < _points.Count; i++)
            linePoints[i] = new PointF(XAt(i, plot), YAt(_points[i].Value, min, max, plot));

        // 面积和折线至少需要两个点；只有一个点时先跳过，避免 GDI+ 抛异常。
        if (linePoints.Length >= 2)
        {
            // ---------- 半透明面积 + 折线 ----------
            using var areaPath = new GraphicsPath();
            areaPath.AddLines(linePoints);
            areaPath.AddLine(linePoints[^1].X, plot.Bottom, linePoints[0].X, plot.Bottom);
            areaPath.CloseFigure();

            // 颜色里的 26 是透明度（0 全透明，255 不透明）。
            using var areaBrush = new SolidBrush(Color.FromArgb(26, UiTheme.Accent));
            g.FillPath(areaBrush, areaPath);

            using var linePen = new Pen(UiTheme.Accent, 2.2f) { LineJoin = LineJoin.Round };
            g.DrawLines(linePen, linePoints);
        }

        // 最新点画实心圆，强调“当前值”。
        var last = linePoints[^1];
        using var lastBrush = new SolidBrush(UiTheme.Accent);
        g.FillEllipse(lastBrush, last.X - 4, last.Y - 4, 8, 8);

        if (_hoverIndex >= 0 && _hoverIndex < _points.Count)
            DrawHover(g, plot, linePoints[_hoverIndex]);
    }

    /// <summary>
    /// 画悬停效果：虚线十字、圆点标记、提示框。
    /// 提示框默认在点的右上方；放不下时自动翻到左边或下边。
    /// </summary>
    private void DrawHover(Graphics g, RectangleF plot, PointF point)
    {
        using var crossPen = new Pen(Color.FromArgb(120, UiTheme.Muted)) { DashStyle = DashStyle.Dash };
        g.DrawLine(crossPen, point.X, plot.Top, point.X, plot.Bottom);
        g.DrawLine(crossPen, plot.Left, point.Y, plot.Right, point.Y);

        using var markerBrush = new SolidBrush(UiTheme.Alarm);
        using var markerPen = new Pen(Color.White, 2f);
        g.FillEllipse(markerBrush, point.X - 5, point.Y - 5, 10, 10);
        g.DrawEllipse(markerPen, point.X - 5, point.Y - 5, 10, 10);

        var p = _points[_hoverIndex];
        var title = p.Time.ToString("HH:mm:ss");
        var value = p.Value.ToString("F2");
        var titleSize = g.MeasureString(title, _tipFont);
        var valueSize = g.MeasureString(value, _tipFont);

        var width = Math.Max(titleSize.Width, valueSize.Width) + 18;
        var height = titleSize.Height + valueSize.Height + 14;

        var x = point.X + 12;
        if (x + width > plot.Right - 2) x = point.X - width - 12;
        x = Math.Max(plot.Left + 2, x);

        var y = point.Y - height - 12;
        if (y < plot.Top + 2) y = point.Y + 12;
        y = Math.Min(plot.Bottom - height - 2, y);

        var box = new RectangleF(x, y, width, height);
        using var boxBrush = new SolidBrush(Color.FromArgb(245, 255, 255, 255));
        using var boxPen = new Pen(UiTheme.Border);
        g.FillRectangle(boxBrush, box);
        g.DrawRectangle(boxPen, x, y, width, height);

        using var textBrush = new SolidBrush(UiTheme.Text);
        using var mutedBrush = new SolidBrush(UiTheme.Muted);
        g.DrawString(title, _tipFont, mutedBrush, x + 9, y + 5);
        g.DrawString(value, _tipFont, textBrush, x + 9, y + 7 + titleSize.Height);
    }

    /// <summary>绘图区：左留 58px 给 Y 轴文字，下留 58px 给时间文字，右留 20px。</summary>
    private RectangleF PlotRect() => new(58, 18, Math.Max(0, Width - 78), Math.Max(0, Height - 58));

    /// <summary>第 index 个点的 X 像素坐标：把下标均匀铺满绘图区宽度。</summary>
    private float XAt(int index, RectangleF plot) =>
        _points.Count <= 1 ? plot.Left + plot.Width / 2 : plot.Left + plot.Width * index / (_points.Count - 1);

    /// <summary>数值 → Y 像素坐标；用 plot.Bottom 减，是因为屏幕 Y 轴向下增大。</summary>
    private static float YAt(double value, double min, double max, RectangleF plot) =>
        (float)(plot.Bottom - (value - min) / (max - min) * plot.Height);

    /// <summary>释放自建字体，避免 GDI 资源泄漏。</summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _axisFont.Dispose();
            _tipFont.Dispose();
        }
        base.Dispose(disposing);
    }

    /// <summary>一个数据点：时间 + 数值。结构体足够轻量，不需要单独建类。</summary>
    private readonly record struct TrendPoint(DateTime Time, double Value);
}
