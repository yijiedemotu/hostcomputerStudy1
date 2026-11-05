namespace Exercises.WinForms.Controls;

/// <summary>
/// 第 04 章练习 5 的演示窗口：点关闭不退出，而是隐藏到托盘；双击托盘恢复。
///
/// 关键点：
/// 1. NotifyIcon 负责在系统托盘区显示图标和右键菜单；
/// 2. FormClosing 里拦截“用户点 ×”，改为 Hide()；
/// 3. 托盘双击恢复窗口；
/// 4. 必须提供“退出程序”入口，否则窗口隐藏后用户没法结束进程；
/// 5. 退出时隐藏并 Dispose 托盘图标，否则图标会残留在托盘区，鼠标划过才消失。
/// </summary>
internal sealed class TrayDemoForm : Form
{
    private readonly NotifyIcon _trayIcon;              // 托盘图标
    private readonly ContextMenuStrip _trayMenu;        // 托盘右键菜单
    private readonly System.Windows.Forms.Timer _uptimeTimer;   // 每秒更新运行时长
    private readonly Label _uptimeLabel;
    private readonly DateTime _launchedAt = DateTime.Now;

    // 标记“用户是不是真的想退出”。
    // false 时点 × 只是隐藏；true 时才允许窗体真正关闭。
    private bool _exitRequested;

    public TrayDemoForm()
    {
        Text = "练习 04-5 · 托盘演示窗口";
        ClientSize = new Size(520, 330);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = UiTheme.Surface;
        Font = UiTheme.BodyFont;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowIcon = false;

        // ---------- 界面文字 ----------
        var title = new Label
        {
            Text = "采集程序正在运行",
            Font = new Font("Microsoft YaHei UI", 16F, FontStyle.Bold),
            ForeColor = UiTheme.Text,
            AutoSize = true,
            Location = new Point(28, 26)
        };
        Controls.Add(title);

        var badge = new Label
        {
            Text = "● 运行中",
            Font = UiTheme.BodyFont,
            ForeColor = Color.FromArgb(21, 128, 61),
            AutoSize = true,
            Location = new Point(30, 64)
        };
        Controls.Add(badge);

        _uptimeLabel = new Label
        {
            Text = "",
            Font = UiTheme.MonoFont,
            ForeColor = UiTheme.Muted,
            AutoSize = true,
            Location = new Point(110, 65)
        };
        Controls.Add(_uptimeLabel);

        var explanation = new Label
        {
            Text = "现在点窗口右上角的 ×，窗口会隐藏到系统托盘，程序没有退出。\r\n" +
                   "双击托盘图标可以恢复窗口；右键托盘图标选择“退出程序”才会结束进程。",
            Font = UiTheme.BodyFont,
            ForeColor = UiTheme.Muted,
            AutoSize = true,
            MaximumSize = new Size(468, 0),
            Location = new Point(30, 108)
        };
        Controls.Add(explanation);

        // ---------- 两个按钮 ----------
        var buttons = new FlowLayoutPanel
        {
            Location = new Point(24, 208),
            Size = new Size(472, 42),
            WrapContents = false,
            BackColor = UiTheme.Surface
        };
        var hideButton = UiTheme.Button("隐藏到托盘", primary: true);
        hideButton.Click += (_, _) => HideToTray();
        buttons.Controls.Add(hideButton);

        var closeButton = UiTheme.Button("关闭窗口（也会隐藏）");
        closeButton.Click += (_, _) => Close();   // Close 会走 FormClosing，被拦截后变成隐藏
        buttons.Controls.Add(closeButton);
        Controls.Add(buttons);

        var tip = new Label
        {
            Text = "提示：托盘图标用的是 SystemIcons.Application，生产程序应换成自己的 .ico，并在退出时 Dispose。",
            Font = UiTheme.BodyFont,
            ForeColor = UiTheme.Muted,
            AutoSize = true,
            MaximumSize = new Size(468, 0),
            Location = new Point(30, 262)
        };
        Controls.Add(tip);

        // ---------- 托盘右键菜单 ----------
        _trayMenu = new ContextMenuStrip();
        _trayMenu.Items.Add("显示窗口", null, (_, _) => RestoreFromTray());
        _trayMenu.Items.Add(new ToolStripSeparator());
        _trayMenu.Items.Add("退出程序", null, (_, _) => ExitFromTray());

        // ---------- 托盘图标 ----------
        // Icon 使用系统自带图标，省去准备 .ico 文件；生产程序应换成自己的图标。
        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "WinForms 练习 · 双击恢复窗口",   // 鼠标悬停时显示的文字
            Visible = true,
            ContextMenuStrip = _trayMenu,
            BalloonTipTitle = "程序仍在运行",
            BalloonTipText = "窗口已隐藏到托盘，双击图标可恢复。"
        };
        _trayIcon.DoubleClick += (_, _) => RestoreFromTray();

        // 每秒刷新“已运行多久”，证明窗口隐藏时程序仍在工作。
        _uptimeTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _uptimeTimer.Tick += (_, _) => UpdateUptime();
        _uptimeTimer.Start();
        UpdateUptime();
    }

    /// <summary>
    /// 窗体即将关闭时调用。
    /// 只拦截“用户点 ×”（UserClosing）；如果主窗体关闭或系统关机，必须放行。
    /// </summary>
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing && !_exitRequested)
        {
            e.Cancel = true;   // 取消真正的关闭
            HideToTray();      // 改为隐藏
        }
        base.OnFormClosing(e);
    }

    /// <summary>窗体真正关闭后，释放所有托盘相关资源，避免图标残留。</summary>
    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _uptimeTimer.Stop();
        _uptimeTimer.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _trayMenu.Dispose();
        base.OnFormClosed(e);
    }

    /// <summary>隐藏窗口，同时让任务栏按钮也消失。</summary>
    private void HideToTray()
    {
        Hide();
        ShowInTaskbar = false;

        // 气球提示在部分系统上受“通知”设置影响，不显示也不影响托盘功能。
        _trayIcon.ShowBalloonTip(2500);
    }

    /// <summary>从托盘恢复：显示任务栏按钮 → 显示窗口 → 若最小化则还原 → 激活到前台。</summary>
    private void RestoreFromTray()
    {
        ShowInTaskbar = true;
        Show();

        if (WindowState == FormWindowState.Minimized)
            WindowState = FormWindowState.Normal;

        Activate();   // 获取焦点，窗口出现在最前面
    }

    /// <summary>供外部页面调用：把已打开的演示窗口恢复到前台。</summary>
    internal void RestoreWindow() => RestoreFromTray();

    /// <summary>供外部页面调用：真正关闭窗口（先置退出标志，避免又被拦截）。</summary>
    internal void ExitWindow()
    {
        _exitRequested = true;
        Close();
    }

    /// <summary>托盘菜单“退出程序”：先隐藏图标，再真正关闭。</summary>
    private void ExitFromTray()
    {
        _exitRequested = true;
        _trayIcon.Visible = false;
        Close();
    }

    /// <summary>刷新运行时长。转义符 \\: 表示输出一个冒号，hh\:mm\:ss 是时:分:秒格式。</summary>
    private void UpdateUptime()
    {
        var span = DateTime.Now - _launchedAt;
        _uptimeLabel.Text = $"已运行 {span:hh\\:mm\\:ss}（窗口隐藏时也在计时）";
    }
}
