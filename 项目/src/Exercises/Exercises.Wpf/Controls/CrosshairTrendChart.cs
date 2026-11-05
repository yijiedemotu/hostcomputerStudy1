using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Exercises.Wpf.Models;

namespace Exercises.Wpf.Controls;

/// <summary>
/// 第 05 章练习 2：WPF 版趋势图 + 鼠标十字光标。
/// 曲线、网格、提示框全部在 OnRender 里画，不依赖任何图表库。
///
/// WPF 自绘控件的三个关键：
/// 1. 继承 FrameworkElement，重写 OnRender(DrawingContext dc)；
/// 2. 需要“外部可绑定”的属性，用 DependencyProperty 注册（普通属性无法用 Binding 赋值）；
/// 3. 数据变化时调用 InvalidateVisual()，WPF 会重新调用 OnRender。
///
/// 颜色不写死，而是通过 SetResourceReference 引用主题资源；
/// 换肤时资源字典一变，图表颜色也会自动跟着变。
/// </summary>
public sealed class CrosshairTrendChart : FrameworkElement
{
    // FormattedText 需要字体信息：普通文字和加粗文字各准备一个。
    private readonly Typeface _normalTypeface = new(
        new FontFamily("Microsoft YaHei UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

    private readonly Typeface _boldTypeface = new(
        new FontFamily("Microsoft YaHei UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);

    // 当前鼠标悬停的数据点下标；-1 表示没有悬停。
    private int _hoverIndex = -1;

    public CrosshairTrendChart()
    {
        ClipToBounds = true;          // 超出控件范围的内容裁掉
        Cursor = Cursors.Cross;       // 十字鼠标
        SnapsToDevicePixels = true;   // 线条对齐像素，避免模糊

        // SetResourceReference：让依赖属性跟随主题资源。
        // 第二个参数是 Themes/Dark.xaml、Light.xaml 里定义的资源 Key。
        // 主题切换后，这些 Brush 会自动变成新主题的颜色。
        SetResourceReference(LineBrushProperty, "AccentBrush");
        SetResourceReference(PlotBrushProperty, "SurfaceAltBrush");
        SetResourceReference(TooltipBrushProperty, "SurfaceBrush");
        SetResourceReference(GridBrushProperty, "GridBrush");
        SetResourceReference(BorderBrushProperty, "BorderBrush");
        SetResourceReference(TextBrushProperty, "TextBrush");
        SetResourceReference(MutedBrushProperty, "MutedBrush");
        SetResourceReference(AlarmBrushProperty, "AlarmBrush");

        // WPF 事件也是普通 .NET 事件；这里挂鼠标移动/移出和尺寸变化。
        MouseMove += OnMouseMoveHandler;
        MouseLeave += (_, _) => ClearHover();
        SizeChanged += (_, _) => InvalidateVisual();
    }

    /// <summary>
    /// 注册依赖属性 Points。
    /// AffectsRender：值变化时自动重新渲染；
    /// OnPointsChanged：值变化时额外挂/摘集合的 CollectionChanged 事件。
    /// </summary>
    public static readonly DependencyProperty PointsProperty = DependencyProperty.Register(
        nameof(Points), typeof(ObservableCollection<TrendPoint>), typeof(CrosshairTrendChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnPointsChanged));

    public ObservableCollection<TrendPoint>? Points
    {
        get => (ObservableCollection<TrendPoint>?)GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    // 下面每个颜色都是 DependencyProperty。
    // 写法看起来重复，但这是 WPF 的规定：
    // 只有依赖属性才能被 XAML 赋值、被 Binding 绑定、被样式/主题资源影响。
    // AffectsRender 表示属性变化时自动重绘，不必手动调 InvalidateVisual。
    public static readonly DependencyProperty LineBrushProperty = DependencyProperty.Register(
        nameof(LineBrush), typeof(Brush), typeof(CrosshairTrendChart),
        new FrameworkPropertyMetadata(Brushes.DodgerBlue, FrameworkPropertyMetadataOptions.AffectsRender));

    public Brush LineBrush
    {
        get => (Brush)GetValue(LineBrushProperty);
        set => SetValue(LineBrushProperty, value);
    }

    public static readonly DependencyProperty PlotBrushProperty = DependencyProperty.Register(
        nameof(PlotBrush), typeof(Brush), typeof(CrosshairTrendChart), new FrameworkPropertyMetadata(Brushes.Transparent));

    public Brush PlotBrush
    {
        get => (Brush)GetValue(PlotBrushProperty);
        set => SetValue(PlotBrushProperty, value);
    }

    public static readonly DependencyProperty TooltipBrushProperty = DependencyProperty.Register(
        nameof(TooltipBrush), typeof(Brush), typeof(CrosshairTrendChart), new FrameworkPropertyMetadata(Brushes.White));

    public Brush TooltipBrush
    {
        get => (Brush)GetValue(TooltipBrushProperty);
        set => SetValue(TooltipBrushProperty, value);
    }

    public static readonly DependencyProperty GridBrushProperty = DependencyProperty.Register(
        nameof(GridBrush), typeof(Brush), typeof(CrosshairTrendChart), new FrameworkPropertyMetadata(Brushes.Gray));

    public Brush GridBrush
    {
        get => (Brush)GetValue(GridBrushProperty);
        set => SetValue(GridBrushProperty, value);
    }

    public static readonly DependencyProperty BorderBrushProperty = DependencyProperty.Register(
        nameof(BorderBrush), typeof(Brush), typeof(CrosshairTrendChart), new FrameworkPropertyMetadata(Brushes.Gray));

    public Brush BorderBrush
    {
        get => (Brush)GetValue(BorderBrushProperty);
        set => SetValue(BorderBrushProperty, value);
    }

    public static readonly DependencyProperty TextBrushProperty = DependencyProperty.Register(
        nameof(TextBrush), typeof(Brush), typeof(CrosshairTrendChart), new FrameworkPropertyMetadata(Brushes.Black));

    public Brush TextBrush
    {
        get => (Brush)GetValue(TextBrushProperty);
        set => SetValue(TextBrushProperty, value);
    }

    public static readonly DependencyProperty MutedBrushProperty = DependencyProperty.Register(
        nameof(MutedBrush), typeof(Brush), typeof(CrosshairTrendChart), new FrameworkPropertyMetadata(Brushes.Gray));

    public Brush MutedBrush
    {
        get => (Brush)GetValue(MutedBrushProperty);
        set => SetValue(MutedBrushProperty, value);
    }

    public static readonly DependencyProperty AlarmBrushProperty = DependencyProperty.Register(
        nameof(AlarmBrush), typeof(Brush), typeof(CrosshairTrendChart), new FrameworkPropertyMetadata(Brushes.OrangeRed));

    public Brush AlarmBrush
    {
        get => (Brush)GetValue(AlarmBrushProperty);
        set => SetValue(AlarmBrushProperty, value);
    }

    /// <summary>
    /// Points 属性被替换时执行：
    /// - 摘掉旧集合的 CollectionChanged；
    /// - 挂上新集合的 CollectionChanged；
    /// - 重绘。
    ///
    /// 只监听属性本身不够：集合对象没变、只是 Add/Remove 了元素时，
    /// 依赖属性不会触发，必须额外监听集合的变化。
    /// </summary>
    private static void OnPointsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not CrosshairTrendChart chart) return;

        if (e.OldValue is INotifyCollectionChanged oldPoints)
            oldPoints.CollectionChanged -= chart.OnPointsCollectionChanged;
        if (e.NewValue is INotifyCollectionChanged newPoints)
            newPoints.CollectionChanged += chart.OnPointsCollectionChanged;

        chart._hoverIndex = -1;
        chart.InvalidateVisual();
    }

    /// <summary>集合增删元素时：修正悬停下标并重绘。</summary>
    private void OnPointsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (Points is not null && _hoverIndex >= Points.Count)
            _hoverIndex = -1;
        InvalidateVisual();
    }

    /// <summary>
    /// 鼠标移动：和 WinForms 版思路相同，把 X 像素位置换算成最近的数据点下标。
    /// e.GetPosition(this) 得到相对于本控件的坐标（不是屏幕坐标）。
    /// </summary>
    private void OnMouseMoveHandler(object sender, MouseEventArgs e)
    {
        var points = Points;
        if (points is null || points.Count == 0) return;

        var plot = PlotRect();
        var position = e.GetPosition(this);
        if (!plot.Contains(position))
        {
            ClearHover();
            return;
        }

        var ratio = Math.Clamp((position.X - plot.Left) / plot.Width, 0, 1);
        var index = Math.Clamp((int)Math.Round(ratio * (points.Count - 1)), 0, points.Count - 1);
        if (index == _hoverIndex) return;

        _hoverIndex = index;
        InvalidateVisual();
    }

    private void ClearHover()
    {
        if (_hoverIndex == -1) return;
        _hoverIndex = -1;
        InvalidateVisual();
    }

    /// <summary>
    /// 绘制入口：WPF 需要重画时调用。
    /// DrawingContext 提供画线、画矩形、画几何图形、画文字的方法。
    /// 和 WinForms 的 Graphics 类似，但更偏向“声明式”的保留模式。
    /// </summary>
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        var points = Points;
        var plot = PlotRect();
        if (plot.Width < 40 || plot.Height < 40) return;   // 控件太小时不画

        // 绘图区背景 + 圆角边框。
        dc.DrawRoundedRectangle(PlotBrush, new Pen(BorderBrush, 1), plot, 6, 6);

        // 没有数据：居中显示“等待数据…”。
        if (points is null || points.Count == 0)
        {
            var empty = Text("等待数据…", 12, MutedBrush);
            dc.DrawText(empty, new Point(
                plot.Left + (plot.Width - empty.Width) / 2,
                plot.Top + (plot.Height - empty.Height) / 2));
            return;
        }

        // 计算 Y 轴范围，上下留 12% 余量。
        var min = points.Min(point => point.Value);
        var max = points.Max(point => point.Value);
        if (Math.Abs(max - min) < 0.001)
        {
            min -= 1;
            max += 1;
        }

        var padding = (max - min) * 0.12;
        min -= padding;
        max += padding;
        var range = max - min;

        // 三步：网格/坐标 → 曲线/面积 → 悬停十字线。
        DrawGridAndAxes(dc, plot, min, max, range, points);
        var positions = DrawCurve(dc, plot, points, min, max);

        if (_hoverIndex >= 0 && _hoverIndex < points.Count)
            DrawCrosshair(dc, plot, points[_hoverIndex], positions[_hoverIndex]);
    }

    /// <summary>画横向网格、Y 轴刻度、X 轴时间标签和单位。</summary>
    private void DrawGridAndAxes(DrawingContext dc, Rect plot, double min, double max,
        double range, ObservableCollection<TrendPoint> points)
    {
        var gridPen = new Pen(GridBrush, 1);
        for (var i = 0; i <= 4; i++)
        {
            // 把高度四等分；最上面是最大值，最下面是最小值。
            var y = plot.Top + plot.Height * i / 4;
            dc.DrawLine(gridPen, new Point(plot.Left, y), new Point(plot.Right, y));

            var value = max - range * i / 4;
            var label = Text(value.ToString("F1"), 10.5, MutedBrush);
            dc.DrawText(label, new Point(plot.Left - label.Width - 10, y - label.Height / 2));
        }

        // 时间轴只标三个位置：起点、中点、终点，避免文字挤在一起。
        var indices = new[] { 0, points.Count / 2, points.Count - 1 }.Distinct().ToArray();
        foreach (var index in indices)
        {
            var x = XAt(index, points.Count, plot);
            var label = Text(points[index].Time.ToString("HH:mm:ss"), 10.5, MutedBrush);
            var left = index == 0 ? x
                : index == points.Count - 1 ? x - label.Width
                : x - label.Width / 2;
            dc.DrawText(label, new Point(Math.Clamp(left, plot.Left, plot.Right - label.Width), plot.Bottom + 8));
        }

        var unit = Text("℃", 10.5, MutedBrush);
        dc.DrawText(unit, new Point(plot.Right - unit.Width, 1));
    }

    /// <summary>
    /// 画面积和折线，返回每个点的屏幕坐标（供悬停使用）。
    ///
    /// StreamGeometry 是 WPF 里高效的几何图形：
    /// 先用 Open() 拿到上下文，BeginFigure/LineTo 描述路径，关闭后 Freeze() 冻结。
    /// Freeze 后对象不可修改，但渲染更快、可跨线程共享。
    /// </summary>
    private Point[] DrawCurve(DrawingContext dc, Rect plot, ObservableCollection<TrendPoint> points,
        double min, double max)
    {
        // 数据坐标 → 屏幕坐标。
        var positions = new Point[points.Count];
        for (var i = 0; i < points.Count; i++)
        {
            positions[i] = new Point(
                XAt(i, points.Count, plot),
                plot.Bottom - (points[i].Value - min) / (max - min) * plot.Height);
        }

        // 面积路径：从左下角开始 → 依次经过每个点 → 到右下角 → 闭合。
        var area = new StreamGeometry();
        using (var context = area.Open())
        {
            context.BeginFigure(new Point(positions[0].X, plot.Bottom), true, true);
            foreach (var point in positions)
                context.LineTo(point, true, false);
            context.LineTo(new Point(positions[^1].X, plot.Bottom), true, false);
        }
        area.Freeze();

        // 半透明填充，颜色由折线颜色自动派生。
        var areaBrush = CreateAreaBrush(LineBrush);
        dc.DrawGeometry(areaBrush, null, area);

        // 折线路径。
        var line = new StreamGeometry();
        using (var context = line.Open())
        {
            context.BeginFigure(positions[0], false, false);
            for (var i = 1; i < positions.Length; i++)
                context.LineTo(positions[i], true, false);
        }
        line.Freeze();

        var linePen = new Pen(LineBrush, 2.2) { LineJoin = PenLineJoin.Round };
        dc.DrawGeometry(null, linePen, line);

        // 最新点画一个圆。
        var last = positions[^1];
        dc.DrawEllipse(LineBrush, new Pen(TooltipBrush, 1.5), last, 4.5, 4.5);

        return positions;
    }

    /// <summary>画十字虚线、标记点和“时间 + 数值”提示框；自动避开绘图区边缘。</summary>
    private void DrawCrosshair(DrawingContext dc, Rect plot, TrendPoint point, Point position)
    {
        var crossPen = new Pen(MutedBrush, 1) { DashStyle = DashStyles.Dash };
        dc.DrawLine(crossPen, new Point(position.X, plot.Top), new Point(position.X, plot.Bottom));
        dc.DrawLine(crossPen, new Point(plot.Left, position.Y), new Point(plot.Right, position.Y));

        dc.DrawEllipse(AlarmBrush, new Pen(TooltipBrush, 2), position, 5, 5);

        var time = Text(point.Time.ToString("HH:mm:ss"), 10.5, MutedBrush);
        var value = Text($"{point.Value:F2} ℃", 13, TextBrush, bold: true);
        var boxWidth = Math.Max(time.Width, value.Width) + 24;
        var boxHeight = time.Height + value.Height + 18;

        // 默认放右上方；放不下就翻到左边/下边，并夹在绘图区内。
        var x = position.X + 14;
        if (x + boxWidth > plot.Right - 2)
            x = position.X - boxWidth - 14;
        x = Math.Clamp(x, plot.Left + 2, plot.Right - boxWidth - 2);

        var y = position.Y - boxHeight - 14;
        if (y < plot.Top + 2)
            y = position.Y + 14;
        y = Math.Clamp(y, plot.Top + 2, plot.Bottom - boxHeight - 2);

        var box = new Rect(x, y, boxWidth, boxHeight);
        dc.DrawRoundedRectangle(TooltipBrush, new Pen(BorderBrush, 1), box, 8, 8);
        dc.DrawText(time, new Point(x + 12, y + 8));
        dc.DrawText(value, new Point(x + 12, y + 10 + time.Height));
    }

    /// <summary>
    /// 创建 FormattedText。WPF 画文字必须提供字体、字号、颜色、DPI 等信息。
    /// PixelsPerDip 用当前屏幕 DPI 计算，保证高分屏文字不模糊。
    /// </summary>
    private FormattedText Text(string text, double size, Brush brush, bool bold = false) =>
        new(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
            bold ? _boldTypeface : _normalTypeface, size, brush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

    /// <summary>从折线颜色派生半透明填充色；如果不是纯色画刷就返回透明。</summary>
    private static Brush CreateAreaBrush(Brush lineBrush)
    {
        if (lineBrush is not SolidColorBrush solid) return Brushes.Transparent;
        var area = new SolidColorBrush(Color.FromArgb(30, solid.Color.R, solid.Color.G, solid.Color.B));
        area.Freeze();
        return area;
    }

    /// <summary>第 index 个点的 X 坐标：把点均匀铺满绘图区宽度。</summary>
    private static double XAt(int index, int count, Rect plot) =>
        count <= 1 ? plot.Left + plot.Width / 2 : plot.Left + plot.Width * index / (count - 1);

    /// <summary>绘图区：左边留 58px 给 Y 轴文字，底部留 56px 给时间文字。</summary>
    private Rect PlotRect() => new(58, 16, Math.Max(0, ActualWidth - 76), Math.Max(0, ActualHeight - 56));
}
