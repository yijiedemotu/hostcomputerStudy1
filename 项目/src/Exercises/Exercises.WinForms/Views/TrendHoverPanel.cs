using Exercises.WinForms.Controls;

namespace Exercises.WinForms.Views;

/// <summary>
/// 第 04 章练习 3：趋势图鼠标悬停显示数值。
///
/// 分工：
/// - 本页面负责“产生数据”：每 500ms 生成一个温度点，更新当前值标签；
/// - HoverTrendChart 负责“画图 + 响应鼠标”：曲线、坐标轴、十字线、提示框都由它自绘。
///
/// 为什么不用现成图表库：
/// 学习阶段手写一遍，才能理解“数据 → 坐标 → 像素”的换算；
/// 生产项目里可以用 LiveCharts、ScottPlot 等库，效率更高。
/// </summary>
internal sealed class TrendHoverPanel : UserControl
{
    private readonly HoverTrendChart _chart;
    private readonly System.Windows.Forms.Timer _timer;   // 每 500ms 产生一个模拟点
    private readonly Label _currentLabel;                 // 当前温度大字
    private readonly Label _timeLabel;                    // 最近一次采样时间
    private readonly Label _statusLabel;                  // “采集中/已暂停”
    private readonly Button _toggleButton;
    private readonly Random _random = new();

    private DateTime _startedAt = DateTime.Now;           // 用于生成平滑的正弦曲线

    public TrendHoverPanel()
    {
        var page = UiTheme.CreatePage(
            "练习 04-3 · 曲线悬停显示数值",
            "自绘控件里重写 OnMouseMove，把鼠标 X 坐标反算成数据点下标，画出十字虚线和“时间 + 数值”提示框；鼠标移出后清除。",
            out var content);
        Dock = DockStyle.Fill;
        Controls.Add(page);

        // 三行：当前值卡片（82px）、工具栏（48px）、图表（剩余空间）。
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = UiTheme.Surface
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // ---------- 顶部当前值卡片 ----------
        var stats = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(248, 250, 252),
            Padding = new Padding(16, 10, 16, 8),
            Margin = new Padding(0)
        };

        // Paint 事件里自绘一个 1 像素边框；using 保证 Pen 用完释放。
        stats.Paint += (_, e) =>
        {
            using var pen = new Pen(UiTheme.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, stats.Width - 1, stats.Height - 1);
        };
        stats.Controls.Add(new Label
        {
            Text = "当前温度",
            Font = UiTheme.BodyFont,
            ForeColor = UiTheme.Muted,
            AutoSize = true,
            Location = new Point(16, 10)
        });
        _currentLabel = new Label
        {
            Text = "--.- ℃",
            Font = UiTheme.ResultFont,
            ForeColor = UiTheme.Accent,
            AutoSize = true,
            Location = new Point(14, 30)
        };
        stats.Controls.Add(_currentLabel);
        _timeLabel = new Label
        {
            Text = "等待采集",
            Font = UiTheme.MonoFont,
            ForeColor = UiTheme.Text,
            AutoSize = true,
            Location = new Point(210, 34)
        };
        stats.Controls.Add(_timeLabel);
        stats.Controls.Add(new Label
        {
            Text = "悬停曲线任意位置可查看历史点，移出图表提示自动消失",
            Font = UiTheme.BodyFont,
            ForeColor = UiTheme.Muted,
            AutoSize = true,
            Location = new Point(210, 55)
        });
        layout.Controls.Add(stats, 0, 0);

        // ---------- 工具栏 ----------
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 8, 0, 6),
            WrapContents = false,
            BackColor = UiTheme.Surface,
            Margin = new Padding(0)
        };
        _toggleButton = UiTheme.Button("暂停", primary: true);
        _toggleButton.Click += (_, _) => ToggleTimer();
        toolbar.Controls.Add(_toggleButton);

        var clearButton = UiTheme.Button("清空曲线");
        clearButton.Click += (_, _) => ClearChart();
        toolbar.Controls.Add(clearButton);

        _statusLabel = new Label
        {
            Text = "采集中 · 500 ms/点 · 保留最近 120 点",
            AutoSize = true,
            Font = UiTheme.BodyFont,
            ForeColor = UiTheme.Muted,
            Margin = new Padding(8, 9, 0, 0)
        };
        toolbar.Controls.Add(_statusLabel);
        layout.Controls.Add(toolbar, 0, 1);

        // ---------- 自绘图表 ----------
        _chart = new HoverTrendChart(maxPoints: 120)
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0)
        };
        layout.Controls.Add(_chart, 0, 2);
        content.Controls.Add(layout);

        // WinForms Timer 的 Tick 在 UI 线程执行，所以可以直接更新控件。
        _timer = new System.Windows.Forms.Timer { Interval = 500 };
        _timer.Tick += (_, _) => AppendPoint();
        _timer.Start();
        AppendPoint();   // 先放一个点，避免打开就是空图
    }

    /// <summary>暂停/继续定时器，并同步按钮文字和状态提示。</summary>
    private void ToggleTimer()
    {
        if (_timer.Enabled)
        {
            _timer.Stop();
            _toggleButton.Text = "继续";
            _statusLabel.Text = "已暂停 · 曲线上仍可悬停查看";
        }
        else
        {
            _timer.Start();
            _toggleButton.Text = "暂停";
            _statusLabel.Text = "采集中 · 500 ms/点 · 保留最近 120 点";
        }
    }

    /// <summary>生成一个模拟温度点并追加到图表。</summary>
    private void AppendPoint()
    {
        var seconds = (DateTime.Now - _startedAt).TotalSeconds;

        // 正弦波让曲线平滑变化，再叠一点随机噪声更像真实测量值。
        // 3.2 * Math.Sin(seconds / 6.0) 的周期约为 37.7 秒。
        var value = 25.0 + 3.2 * Math.Sin(seconds / 6.0) + (_random.NextDouble() - 0.5) * 0.8;

        // 真实项目在这里应替换为“从采集引擎取最新值”。
        _chart.AddPoint(DateTime.Now, value);
        _currentLabel.Text = $"{value:F2} ℃";
        _timeLabel.Text = $"采样时间 {DateTime.Now:HH:mm:ss.fff}";
    }

    /// <summary>清空曲线和显示，重新开始计时。</summary>
    private void ClearChart()
    {
        _chart.ClearPoints();
        _startedAt = DateTime.Now;
        _currentLabel.Text = "--.- ℃";
        _timeLabel.Text = "等待采集";
    }

    /// <summary>
    /// 页面销毁时停掉并释放 Timer。
    /// 忘记释放的 Timer 会继续触发，造成“窗口关了还在后台跑”的隐性泄漏。
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Stop();
            _timer.Dispose();
        }
        base.Dispose(disposing);
    }
}
