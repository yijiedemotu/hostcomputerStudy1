namespace Answers.WinForms.Forms;

/// <summary>
/// 第 04 章练习 3：趋势图鼠标悬停显示数值。
///
/// 分工：
/// - 本窗体负责“产生数据”：每 500ms 生成一个温度点，更新当前值标签；
/// - HoverTrendChart 负责“画图 + 响应鼠标”：
///   曲线、坐标轴、十字线、提示框都由它自己 OnPaint 画出来。
///
/// 为什么不用现成图表库：
/// 学习阶段手写一遍，才能理解“数据 → 坐标 → 像素”的换算。
/// 生产项目可以用 LiveCharts、ScottPlot 等库，效率更高。
/// </summary>
public partial class TrendHoverForm : Form
{
    private readonly Random _random = new();
    private DateTime _startedAt = DateTime.Now;

    public TrendHoverForm()
    {
        InitializeComponent();

        // Timer 放在 Designer.cs 的 components 容器里，
        // 窗体释放时组件会自动 Dispose，不会出现“窗口关了定时器还在跑”。
        timer.Start();
        AppendPoint();   // 先放一个点，避免打开就是空图
    }

    /// <summary>暂停/继续定时器，并同步按钮文字和状态提示。</summary>
    private void btnPause_Click(object? sender, EventArgs e)
    {
        if (timer.Enabled)
        {
            timer.Stop();
            btnPause.Text = "继续";
            btnPause.BackColor = UiTheme.Surface;
            btnPause.ForeColor = UiTheme.Text;
            statusLabel.Text = "已暂停 · 曲线上仍可悬停查看";
        }
        else
        {
            timer.Start();
            btnPause.Text = "暂停";
            btnPause.BackColor = UiTheme.Accent;
            btnPause.ForeColor = Color.White;
            statusLabel.Text = "采集中 · 500 ms/点 · 保留最近 120 点";
        }
    }

    private void btnClear_Click(object? sender, EventArgs e)
    {
        chart.ClearPoints();
        _startedAt = DateTime.Now;
        currentValueLabel.Text = "--.- ℃";
        timeLabel.Text = "等待采集";
    }

    private void timer_Tick(object? sender, EventArgs e) => AppendPoint();

    /// <summary>生成一个模拟温度点并追加到图表。</summary>
    private void AppendPoint()
    {
        var seconds = (DateTime.Now - _startedAt).TotalSeconds;

        // 正弦波让曲线平滑变化，再叠一点随机噪声更像真实测量值。
        // 3.2 * Math.Sin(seconds / 6.0) 的周期约为 37.7 秒。
        var value = 25.0 + 3.2 * Math.Sin(seconds / 6.0) + (_random.NextDouble() - 0.5) * 0.8;

        // 真实项目这里应换成“从采集引擎取最新值”。
        chart.AddPoint(DateTime.Now, value);
        currentValueLabel.Text = $"{value:F2} ℃";
        timeLabel.Text = $"采样时间 {DateTime.Now:HH:mm:ss.fff}";
    }
}
