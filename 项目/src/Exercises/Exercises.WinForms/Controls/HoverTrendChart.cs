using System.Drawing.Drawing2D;

namespace Exercises.WinForms.Controls;

/// <summary>
/// 双缓冲自绘趋势图 + 鼠标悬停十字光标。
/// 对应第 04 章练习 3：鼠标移到曲线上显示“时间 + 数值”。
///
/// 这个控件不依赖任何图表库，全部用 GDI+ 的 Graphics 手画：
/// 1. OnPaint 里依次画背景、坐标轴网格、面积、折线、最新点；
/// 2. 鼠标移动时，把“像素 X 坐标”反算成“第几个数据点”；
/// 3. 再在该点画十字虚线和提示框。
///
/// 坐标系提醒：
/// 屏幕坐标原点在左上角，Y 向下增大；
/// 而温度值是“越大越靠上”，所以 YAt 里要用 plot.Bottom 减去比例。
/// </summary>
internal sealed class HoverTrendChart : Control
{
    private readonly List<TrendPoint> _points = new();   // 当前显示的数据点
    private readonly int _maxPoints;                     // 最多保留多少个点
    private readonly Font _axisFont = new("Microsoft YaHei UI", 8.5F);
    private readonly Font _tipFont = new("Microsoft YaHei UI", 9F, FontStyle.Bold);

    // 当前鼠标悬停的数据点下标；-1 表示没有悬停。
    private int _hoverIndex = -1;

    public HoverTrendChart(int maxPoints = 120)
    {
        _maxPoints = maxPoints;

        // SetStyle 是自定义绘制的关键开关：
        // AllPaintingInWmPaint：所有绘制都在 OnPaint 里完成，减少闪烁；
        // OptimizedDoubleBuffer：双缓冲，先在内存画好再贴屏；
        // UserPaint：由本控件自己画，而不是让系统画默认背景；
        // ResizeRedraw：窗口大小变化时重绘。
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint |
                 ControlStyles.ResizeRedraw, true);
        BackColor = UiTheme.Surface;
        Cursor = Cursors.Cross;   // 鼠标变成十字，提示可以悬停查看
    }

    /// <summary>追加一个数据点；超过上限时丢掉最旧的，形成“滚动窗口”。</summary>
    public void AddPoint(DateTime time, double value)
    {
        _points.Add(new TrendPoint(time, value));

        // 只保留最近 _maxPoints 个点，否则时间越长内存越大、绘制越慢。
        if (_points.Count > _maxPoints)
        {
            var remove = _points.Count - _maxPoints;
            _points.RemoveRange(0, remove);
            _hoverIndex = -1;   // 删除后下标变了，清除悬停状态
        }

        // Invalidate 请求系统重绘；真正绘制会走 OnPaint。
        Invalidate();
    }

    /// <summary>清空所有点并刷新。</summary>
    public void ClearPoints()
    {
        _points.Clear();
        _hoverIndex = -1;
        Invalidate();
    }

    /// <summary>
    /// 鼠标在图表上移动：把像素位置换算成最近的数据点。
    ///
    /// 换算方法：
    /// 1. 鼠标 X 相对于绘图区左边的距离，除以绘图区宽度，得到 0~1 的比例；
    /// 2. 比例乘以 (点数 - 1)，四舍五入，就是最近点的下标。
    /// 例如 10 个点，鼠标在正中间 → 比例 0.5 → 下标 4.5 → 四舍五入 5。
    /// </summary>
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        var plot = PlotRect();

        // 没有数据，或鼠标不在绘图区内：清除悬停提示。
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

        // 下标没变就不用重绘，减少无谓的刷新。
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
    /// 控件的绘制入口。系统需要重画时会调用它（第一次显示、窗口移动、Invalidate 之后等）。
    /// 绘制顺序很重要：后画的会盖住先画的。
    /// </summary>
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;

        // AntiAlias 让曲线和圆点边缘平滑；ClearTypeGridFit 让文字更清晰。
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        // 先用背景色铺满整个控件，避免残留上一帧的内容。
        using var surface = new SolidBrush(BackColor);
        g.FillRectangle(surface, ClientRectangle);

        // 计算“真正画数据”的矩形区域（左边留 Y 轴文字、下边留时间文字）。
        var plot = PlotRect();
        if (plot.Width < 20 || plot.Height < 20) return;   // 控件太小就不画，防止除零/溢出

        // 绘图区底色 + 边框。
        using var plotBg = new SolidBrush(Color.FromArgb(250, 251, 253));
        g.FillRectangle(plotBg, plot);
        using var borderPen = new Pen(UiTheme.Border);
        g.DrawRectangle(borderPen, plot.X, plot.Y, plot.Width, plot.Height);

        // 没有数据时，居中显示“等待数据…”，后面的坐标计算都跳过。
        if (_points.Count == 0)
        {
            using var empty = new SolidBrush(UiTheme.Muted);
            var text = "等待数据…";
            var size = g.MeasureString(text, UiTheme.BodyFont);
            g.DrawString(text, UiTheme.BodyFont, empty,
                plot.Left + (plot.Width - size.Width) / 2,
                plot.Top + (plot.Height - size.Height) / 2);
            return;
        }

        // ---------- 计算 Y 轴范围 ----------
        // 取所有点的最小/最大值；再上下各留 12% 余量，避免曲线贴着边框。
        var min = _points.Min(p => p.Value);
        var max = _points.Max(p => p.Value);

        // 所有值都一样时 max - min = 0，后面除零会出错；手动撑开 2 个单位。
        if (Math.Abs(max - min) < 0.001)
        {
            max += 1;
            min -= 1;
        }
        var padding = (max - min) * 0.12;
        min -= padding;
        max += padding;

        // ---------- 画横向网格和 Y 轴刻度 ----------
        using var gridPen = new Pen(Color.FromArgb(232, 236, 242));
        using var axisBrush = new SolidBrush(UiTheme.Muted);
        using var labelFormat = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
        for (var i = 0; i <= 4; i++)
        {
            // i 从 0 到 4，把高度四等分；y 是从上往下的像素坐标。
            var y = plot.Top + plot.Height * i / 4f;
            g.DrawLine(gridPen, plot.Left, y, plot.Right, y);

            // 最上面是最大值，最下面是最小值，所以要用 max 减去比例。
            var value = max - (max - min) * i / 4;
            g.DrawString(value.ToString("F1"), _axisFont, axisBrush,
                plot.Left - 8, y, labelFormat);
        }

        // ---------- 画 X 轴时间刻度：起点、1/3、2/3、终点 ----------
        using var timeFormat = new StringFormat { Alignment = StringAlignment.Center };
        var labelIndices = new[] { 0, _points.Count / 3, _points.Count * 2 / 3, _points.Count - 1 }
            .Distinct().ToArray();   // 点很少时可能有重复，去重
        foreach (var index in labelIndices)
        {
            var x = XAt(index, plot);

            // 首尾文字分别靠左/靠右，避免超出绘图区被裁掉。
            var alignment = index == 0 ? StringAlignment.Near
                : index == _points.Count - 1 ? StringAlignment.Far
                : StringAlignment.Center;
            timeFormat.Alignment = alignment;
            g.DrawString(_points[index].Time.ToString("HH:mm:ss"), _axisFont, axisBrush,
                x, plot.Bottom + 7, timeFormat);
        }

        // ---------- 把每个数据点换算成屏幕坐标 ----------
        var linePoints = new PointF[_points.Count];
        for (var i = 0; i < _points.Count; i++)
            linePoints[i] = new PointF(XAt(i, plot), YAt(_points[i].Value, min, max, plot));

        // ---------- 先画半透明面积，再画折线，形成“面积图”效果 ----------
        using var areaPath = new GraphicsPath();
        areaPath.AddLines(linePoints);
        areaPath.AddLine(linePoints[^1].X, plot.Bottom, linePoints[0].X, plot.Bottom);   // 右下角
        areaPath.CloseFigure();   // 自动连回起点，闭合图形

        // 颜色里的 26 是透明度（0 全透明，255 不透明）。
        using var areaBrush = new SolidBrush(Color.FromArgb(26, UiTheme.Accent));
        g.FillPath(areaBrush, areaPath);

        // 折线本身：2.2 像素宽，圆角连接。
        using var linePen = new Pen(UiTheme.Accent, 2.2f) { LineJoin = LineJoin.Round };
        g.DrawLines(linePen, linePoints);

        // 最新点画一个实心圆，强调“当前值”。
        var last = linePoints[^1];
        using var lastBrush = new SolidBrush(UiTheme.Accent);
        g.FillEllipse(lastBrush, last.X - 4, last.Y - 4, 8, 8);

        // 有悬停点时，最后画十字线和提示框（保证盖在曲线上）。
        if (_hoverIndex >= 0 && _hoverIndex < _points.Count)
            DrawHover(g, plot, min, max, linePoints[_hoverIndex]);
    }

    /// <summary>
    /// 画悬停效果：横向/纵向虚线、圆点标记、提示框。
    /// 提示框默认放在点的右上方；如果会超出绘图区，就翻到左边或下边。
    /// </summary>
    private void DrawHover(Graphics g, RectangleF plot, double min, double max, PointF point)
    {
        // 虚线十字：横向一条、纵向一条，颜色带 120 的透明度。
        using var crossPen = new Pen(Color.FromArgb(120, UiTheme.Muted)) { DashStyle = DashStyle.Dash };
        g.DrawLine(crossPen, point.X, plot.Top, point.X, plot.Bottom);
        g.DrawLine(crossPen, plot.Left, point.Y, plot.Right, point.Y);

        // 圆点：红底 + 白色描边，醒目但不遮住曲线。
        using var markerBrush = new SolidBrush(UiTheme.Alarm);
        using var markerPen = new Pen(Color.White, 2f);
        g.FillEllipse(markerBrush, point.X - 5, point.Y - 5, 10, 10);
        g.DrawEllipse(markerPen, point.X - 5, point.Y - 5, 10, 10);

        // 取出真实数据，准备画“时间 + 数值”两行文字。
        var p = _points[_hoverIndex];
        var title = p.Time.ToString("HH:mm:ss");
        var value = p.Value.ToString("F2");
        var titleSize = g.MeasureString(title, _tipFont);
        var valueSize = g.MeasureString(value, _tipFont);

        // 提示框宽高按文字测量结果加内边距。
        var width = Math.Max(titleSize.Width, valueSize.Width) + 18;
        var height = titleSize.Height + valueSize.Height + 14;

        // 默认放在点的右上方。
        var x = point.X + 12;
        if (x + width > plot.Right - 2) x = point.X - width - 12;   // 右边放不下 → 放左边
        x = Math.Max(plot.Left + 2, x);                             // 不要超出左边界

        var y = point.Y - height - 12;
        if (y < plot.Top + 2) y = point.Y + 12;                     // 上边放不下 → 放下边
        y = Math.Min(plot.Bottom - height - 2, y);                  // 不要超出下边界

        // 半透明白底提示框 + 细边框。
        var box = new RectangleF(x, y, width, height);
        using var boxBrush = new SolidBrush(Color.FromArgb(245, 255, 255, 255));
        using var boxPen = new Pen(UiTheme.Border);
        g.FillRectangle(boxBrush, box);
        g.DrawRectangle(boxPen, x, y, width, height);

        // 第一行灰色时间，第二行正文颜色数值。
        using var textBrush = new SolidBrush(UiTheme.Text);
        using var mutedBrush = new SolidBrush(UiTheme.Muted);
        g.DrawString(title, _tipFont, mutedBrush, x + 9, y + 5);
        g.DrawString(value, _tipFont, textBrush, x + 9, y + 7 + titleSize.Height);
    }

    /// <summary>
    /// 绘图区：左边留 58px 给 Y 轴文字，下面留 58px 给时间文字，右边留 20px 余量。
    /// Width/Height 是控件当前尺寸，窗口缩放时会自动变化。
    /// </summary>
    private RectangleF PlotRect() => new(58, 18, Math.Max(0, Width - 78), Math.Max(0, Height - 58));

    /// <summary>第 index 个点的 X 像素坐标：把下标均匀铺满绘图区宽度。</summary>
    private float XAt(int index, RectangleF plot) =>
        _points.Count <= 1 ? plot.Left + plot.Width / 2 : plot.Left + plot.Width * index / (_points.Count - 1);

    /// <summary>
    /// 数值 → Y 像素坐标。
    /// (value - min) / (max - min) 得到 0~1 的比例；
    /// 乘以高度后从底部往上量，所以用 plot.Bottom 减。
    /// </summary>
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

    /// <summary>一个数据点：时间 + 数值。</summary>
    private readonly record struct TrendPoint(DateTime Time, double Value);
}
