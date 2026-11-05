using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace ScadaDashboard.Controls;

/// <summary>
/// WPF 趋势图（直接重写 OnRender 用 DrawingContext 画）。
///
/// WinForms 项目里是 GDI+ 的 OnPaint，WPF 这里是矢量渲染：
/// FormattedText 画字、StreamGeometry 画折线、DrawingContext 处理裁剪与变换。
/// 两种技术栈都手写一遍，你就明白“图表”到底是怎么来的。
///
/// 它继承 FrameworkElement 而不是 Control：
///  - 没有默认样式、没有子控件，只需要一个“画布”；
///  - 重写 OnRender 就能拿到 DrawingContext；
///  - 尺寸由 XAML 布局决定（实际宽高在 ActualWidth/ActualHeight 里）。
/// </summary>
public sealed class TrendChartControl : FrameworkElement
{
    // 最多保留 1500 个点。采集 500ms 一次时约等于 12.5 分钟，
    // 既够看趋势，又不会让列表无限增长拖慢绘制。
    private const int MaxPoints = 1500;

    // 画刷和画笔统一在静态字段里创建并 Freeze。
    // Freeze 之后对象不可变，WPF 可以跨线程安全共享，绘制也更快；
    // 如果每次 OnRender 都 new Brush，界面刷新时会产生大量垃圾。
    private static readonly Brush BackgroundBrush = Frozen(Color.FromRgb(0x14, 0x17, 0x1D));
    private static readonly Brush TextBrush = Frozen(Color.FromRgb(0x9A, 0xA3, 0xB2));
    private static readonly Brush TitleBrush = Frozen(Color.FromRgb(0xE6, 0xE8, 0xEC));
    private static readonly Brush AccentBrush = Frozen(Color.FromRgb(0x00, 0xDC, 0xA0));
    private static readonly Brush AlarmBrush = Frozen(Color.FromRgb(0xFF, 0x6B, 0x6B));
    private static readonly Brush LowBrush = Frozen(Color.FromRgb(0x5A, 0xA0, 0xF0));
    private static readonly Pen GridPen = FrozenPen(Color.FromRgb(0x2A, 0x2F, 0x3A), 1);
    private static readonly Pen LinePen = FrozenPen(Color.FromRgb(0x00, 0xDC, 0xA0), 2);
    private static readonly Pen HighPen = DashedPen(Color.FromRgb(0xFF, 0x6B, 0x6B));
    private static readonly Pen LowPen = DashedPen(Color.FromRgb(0x5A, 0xA0, 0xF0));

    // 当前显示的采样点。DateTime 用来画横轴刻度，double 是工程值。
    // 所有增删都在 UI 线程上发生（ViewModel 已经 Post 回来了），所以不用加锁。
    private readonly List<(DateTime Time, double Value)> _points = new();

    /// <summary>图表标题，例如“1#注塑机 / 模温”。</summary>
    public string Title { get; set; } = "未选择位号";

    /// <summary>工程单位，例如“℃”。</summary>
    public string Unit { get; set; } = "";

    /// <summary>下限，画一条蓝色虚线；null 表示不画。</summary>
    public double? LowLimit { get; set; }

    /// <summary>上限，画一条红色虚线；null 表示不画。</summary>
    public double? HighLimit { get; set; }

    /// <summary>
    /// 切换位号时调用：清空旧曲线，换标题/单位/上下限，然后请求重绘。
    /// InvalidateVisual 不会立刻画，而是告诉 WPF“这一帧需要重画”。
    /// </summary>
    public void Reset(string title, string unit, double? lowLimit, double? highLimit)
    {
        _points.Clear();
        Title = title;
        Unit = unit;
        LowLimit = lowLimit;
        HighLimit = highLimit;
        InvalidateVisual();
    }

    /// <summary>
    /// 追加一个采样点。超过上限时从最旧的开始删，
    /// 这样曲线看起来是“向左滚动”，而不是越挤越密。
    /// </summary>
    public void AddPoint(DateTime time, double value)
    {
        _points.Add((time, value));

        if (_points.Count > MaxPoints)
            _points.RemoveRange(0, _points.Count - MaxPoints);

        InvalidateVisual();
    }

    /// <summary>
    /// WPF 的绘制入口。每次需要重画时由 WPF 调用，
    /// 我们拿到 DrawingContext 后按“背景 → 网格 → 曲线 → 文字”的顺序画。
    /// 注意：不要在 OnRender 里创建控件或做耗时业务，它会被频繁调用。
    /// </summary>
    protected override void OnRender(DrawingContext dc)
    {
        var width = ActualWidth;
        var height = ActualHeight;

        // 布局还没完成时宽高可能是 0，直接返回，避免画出奇怪的东西。
        if (width < 40 || height < 40) return;

        // 背景：深色圆角矩形，铺满整个控件。
        dc.DrawRoundedRectangle(BackgroundBrush, null, new Rect(0, 0, width, height), 8, 8);

        // 取当前屏幕 DPI。FormattedText 需要这个值才能把字号换算成像素，
        // 否则在 125%/150% 缩放的屏幕上文字会糊或大小不对。
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        // 左上角标题。
        var titleText = MakeText(Title, 14, TitleBrush, dpi, bold: true);
        dc.DrawText(titleText, new Point(16, 10));

        // 真正的绘图区：左边留 66 像素给 Y 轴刻度，底部留 30 像素给时间刻度。
        // Math.Max 保证窗口被拖得很小时绘图区仍有最小尺寸。
        var plot = new Rect(66, 48, Math.Max(20, width - 66 - 18), Math.Max(20, height - 48 - 30));
        dc.DrawRectangle(null, GridPen, plot);   // 只画边框，不填充

        // 还没有数据：在绘图区里提示用户去点上面的卡片。
        if (_points.Count == 0)
        {
            dc.DrawText(MakeText("等待数据…（点击上方卡片选择位号）", 12, TextBrush, dpi), new Point(plot.Left + 16, plot.Top + 16));
            return;
        }

        // ---- 计算 Y 轴范围 ----
        // 先取数据的最小/最大值，再把上下限也纳入范围，
        // 这样即使当前值离限值很远，限值线也能出现在图里。
        var min = _points.Min(p => p.Value);
        var max = _points.Max(p => p.Value);
        if (HighLimit is { } high) max = Math.Max(max, high);
        if (LowLimit is { } low) min = Math.Min(min, low);

        // 所有值都相同时，max-min 为 0，除法会出错；
        // 人为撑开 ±1，保证曲线画在中间而不是贴边。
        if (Math.Abs(max - min) < 1e-9) { max += 1; min -= 1; }

        // 上下各留 12% 的空白，曲线不会紧贴边框。
        var padding = (max - min) * 0.12;
        min -= padding;
        max += padding;

        // ---- 横向网格 + Y 轴刻度（5 等分，共 6 条线）----
        for (var i = 0; i <= 5; i++)
        {
            // i=0 是最下面（min），i=5 是最上面（max）。
            var y = plot.Bottom - plot.Height * i / 5.0;
            dc.DrawLine(GridPen, new Point(plot.Left, y), new Point(plot.Right, y));

            var label = MakeText((min + (max - min) * i / 5.0).ToString("0.###"), 10, TextBrush, dpi);
            // 让文字垂直居中对齐网格线。
            dc.DrawText(label, new Point(10, y - label.Height / 2));
        }

        // ---- 纵向网格 + 时间刻度（4 等分，共 5 条线）----
        for (var i = 0; i <= 4; i++)
        {
            var x = plot.Left + plot.Width * i / 4.0;
            dc.DrawLine(GridPen, new Point(x, plot.Top), new Point(x, plot.Bottom));

            // 按“第几个点”取时间，而不是按真实时间差。
            // 数据是等间隔采集的，这样最简单；真实项目若间隔不固定，
            // 应该按时间戳映射 X 坐标。
            var index = (int)Math.Round((_points.Count - 1) * i / 4.0);
            var label = MakeText(_points[Math.Clamp(index, 0, _points.Count - 1)].Time.ToString("HH:mm:ss"), 10, TextBrush, dpi);
            dc.DrawText(label, new Point(x - label.Width / 2, plot.Bottom + 4));
        }

        // ---- 上下限虚线 ----
        if (HighLimit is { } highLimit)
        {
            var y = MapY(highLimit, plot, min, max);
            dc.DrawLine(HighPen, new Point(plot.Left, y), new Point(plot.Right, y));
            dc.DrawText(MakeText($"上限 {highLimit:0.###}", 10, AlarmBrush, dpi), new Point(plot.Right - 70, y - 16));
        }
        if (LowLimit is { } lowLimit)
        {
            var y = MapY(lowLimit, plot, min, max);
            dc.DrawLine(LowPen, new Point(plot.Left, y), new Point(plot.Right, y));
            dc.DrawText(MakeText($"下限 {lowLimit:0.###}", 10, LowBrush, dpi), new Point(plot.Right - 70, y + 2));
        }

        // ---- 折线 ----
        // StreamGeometry 是 WPF 里高效的矢量路径：
        // BeginFigure 起头，LineTo 一段段连下去，最后 Freeze 固定下来。
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            for (var i = 0; i < _points.Count; i++)
            {
                // 只有一个点时画在正中间；否则按索引平均分布。
                var x = _points.Count == 1 ? plot.Left + plot.Width / 2 : plot.Left + plot.Width * i / (_points.Count - 1);
                var p = new Point(x, MapY(_points[i].Value, plot, min, max));

                if (i == 0) ctx.BeginFigure(p, false, false);   // 不填充、不闭合
                else ctx.LineTo(p, true, false);                // 连线，参与描边
            }
        }
        geometry.Freeze();

        // 裁剪到绘图区，保证曲线不会画到坐标轴外面。
        // PushClip/Pop 必须成对出现。
        dc.PushClip(new RectangleGeometry(plot));
        dc.DrawGeometry(null, LinePen, geometry);   // 不填充，只描边
        dc.Pop();

        // ---- 右上角当前值 ----
        var last = _points[^1];   // ^1 表示最后一个元素
        var valueText = MakeText($"{last.Value:0.###}", 26, TitleBrush, dpi, bold: true);
        dc.DrawText(valueText, new Point(plot.Right - valueText.Width - 12, 8));

        if (!string.IsNullOrEmpty(Unit))
        {
            var unitText = MakeText(Unit, 12, TextBrush, dpi);
            dc.DrawText(unitText, new Point(plot.Right - unitText.Width - 12, 8 + valueText.Height - 4));
        }

        // 右下角采样点数，方便判断“曲线不动”是数据问题还是界面问题。
        var info = MakeText($"{_points.Count} 点", 10, TextBrush, dpi);
        dc.DrawText(info, new Point(plot.Right - info.Width - 6, plot.Bottom - info.Height - 4));
    }

    /// <summary>
    /// 把工程值换算成绘图区里的 Y 坐标。
    /// WPF 的 Y 轴向下增长，所以要反过来：值越大，Y 越小（越靠上）。
    /// </summary>
    private static double MapY(double value, Rect plot, double min, double max) =>
        plot.Bottom - (value - min) / (max - min) * plot.Height;

    /// <summary>
    /// 创建一段可直接绘制的文字。
    /// FormattedText 是 WPF 的“文字排版对象”，比 DrawString 更底层，
    /// 支持字体、字号、粗细、DPI 和换行控制。
    /// </summary>
    private FormattedText MakeText(string text, double size, Brush brush, double pixelsPerDip, bool bold = false) =>
        new(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Microsoft YaHei UI"), FontStyles.Normal,
                bold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal),
            size, brush, pixelsPerDip);

    /// <summary>创建纯色画刷并 Freeze（不可变、可跨线程共享、渲染更快）。</summary>
    private static Brush Frozen(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    /// <summary>创建实线画笔并 Freeze。</summary>
    private static Pen FrozenPen(Color color, double thickness)
    {
        var pen = new Pen(Frozen(color), thickness);
        pen.Freeze();
        return pen;
    }

    /// <summary>创建虚线画笔：4 像素实线、4 像素空白，用来画上下限。</summary>
    private static Pen DashedPen(Color color)
    {
        var pen = new Pen(Frozen(color), 1) { DashStyle = new DashStyle(new double[] { 4, 4 }, 0) };
        pen.Freeze();
        return pen;
    }
}
