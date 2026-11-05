using Exercises.WinForms.Views;

namespace Exercises.WinForms;

/// <summary>
/// WinForms 练习的主窗口：上方标题栏 + 下方 6 个页签。
///
/// 每个页签对应一道练习，页面本身是 UserControl（见 Views 文件夹）。
/// 主窗口只负责“把页面放进页签”，具体交互都在各自页面里，方便逐个阅读。
/// </summary>
public sealed class MainForm : Form
{
    public MainForm()
    {
        Text = "第 01 / 04 章练习答案 · WinForms";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1180, 780);
        MinimumSize = new Size(1000, 680);
        BackColor = UiTheme.Background;
        Font = UiTheme.BodyFont;

        // 顶部标题区域：固定高度，靠在窗口上方。
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 82,
            BackColor = UiTheme.Surface,
            Padding = new Padding(26, 14, 26, 10)
        };
        header.Controls.Add(new Label
        {
            Text = "WinForms 练习答案",
            Font = new Font("Microsoft YaHei UI", 16F, FontStyle.Bold),
            ForeColor = UiTheme.Text,
            AutoSize = true,
            Location = new Point(24, 14)
        });
        header.Controls.Add(new Label
        {
            Text = "第 01 章小任务 + 第 04 章 5 个练习 · 每个页签都可以直接操作",
            Font = UiTheme.BodyFont,
            ForeColor = UiTheme.Muted,
            AutoSize = true,
            Location = new Point(27, 48)
        });

        // TabControl 就是“页签容器”，每个 TabPage 是一页。
        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Padding = new Point(18, 6),
            Font = UiTheme.BodyFont
        };
        AddTab(tabs, "01 第一个小任务", new FirstTaskPanel());
        AddTab(tabs, "04-1 发送历史", new SendHistoryPanel());
        AddTab(tabs, "04-2 报警筛选", new AlarmFilterPanel());
        AddTab(tabs, "04-3 曲线悬停", new TrendHoverPanel());
        AddTab(tabs, "04-4 VirtualMode 10万行", new VirtualGridPanel());
        AddTab(tabs, "04-5 托盘最小化", new TrayPanel());

        // Dock 的叠放顺序：先添加的控件靠边停靠后加的更“外”。
        // 先加 tabs（Fill），再加 header（Top），最终 header 在上、tabs 填满下面。
        Controls.Add(tabs);
        Controls.Add(header);
    }

    /// <summary>把任意 UserControl 包成页签，并让它填满整页。</summary>
    private static void AddTab(TabControl tabs, string title, Control page)
    {
        var tab = new TabPage(title)
        {
            BackColor = UiTheme.Background,
            Padding = new Padding(0)
        };
        page.Dock = DockStyle.Fill;
        tab.Controls.Add(page);
        tabs.TabPages.Add(tab);
    }
}
