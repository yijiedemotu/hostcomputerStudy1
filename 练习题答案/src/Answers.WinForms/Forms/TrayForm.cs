namespace Answers.WinForms.Forms;

/// <summary>
/// 第 04 章练习 5：窗口关闭时最小化到托盘。
///
/// 关键点：
/// 1. NotifyIcon 提供托盘图标，ContextMenuStrip 提供右键菜单；
/// 2. 用户点窗口右上角 × 时，FormClosing 里取消关闭并 Hide()；
/// 3. 托盘图标双击 → 重新 Show() 并激活；
/// 4. 只有菜单里的“退出程序”才真正 Close()，否则进程会一直留在后台。
///
/// 为什么主窗体用 Show（非模态）打开它：
/// 窗口隐藏到托盘时并没有关闭，ShowDialog 会一直锁住主窗口，
/// 用户就没法继续操作主界面了。
/// </summary>
public partial class TrayForm : Form
{
    private bool _exiting;
    private DateTime _startedAt;

    public TrayForm()
    {
        InitializeComponent();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        _startedAt = DateTime.Now;

        // 设计器里不方便内置 .ico 文件，所以运行时用系统自带的图标。
        // SystemIcons 返回的是共享图标，不能 Dispose。
        notifyIcon.Icon = SystemIcons.Application;
        notifyIcon.Visible = true;

        uptimeTimer.Start();
        UpdateUptime();
    }

    /// <summary>隐藏到托盘。主窗体退出时也会调用它，确保托盘图标一并释放。</summary>
    private void HideToTray()
    {
        Hide();
        ShowInTaskbar = false;
        stateLabel.Text = "已隐藏到托盘 · 双击图标可以恢复";
        stateLabel.ForeColor = UiTheme.Accent;

        if (notifyIcon.Visible)
        {
            notifyIcon.ShowBalloonTip(
                1500,
                "程序仍在运行",
                "双击托盘图标可以恢复窗口；右键菜单可以退出程序。",
                ToolTipIcon.Info);
        }
    }

    /// <summary>从托盘恢复窗口到前台。</summary>
    public void RestoreWindow()
    {
        ShowInTaskbar = true;
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
        BringToFront();

        stateLabel.Text = "窗口显示中 · 点 × 可再次隐藏到托盘";
        stateLabel.ForeColor = UiTheme.Text;
    }

    /// <summary>真正退出。由 MainForm 在关闭时调用，避免留下隐藏进程。</summary>
    public void ExitWindow()
    {
        _exiting = true;
        Close();
    }

    private void btnHide_Click(object? sender, EventArgs e) => HideToTray();

    private void btnExit_Click(object? sender, EventArgs e) => ExitWindow();

    private void notifyIcon_DoubleClick(object? sender, EventArgs e) => RestoreWindow();

    private void menuRestore_Click(object? sender, EventArgs e) => RestoreWindow();

    private void menuExit_Click(object? sender, EventArgs e) => ExitWindow();

    private void uptimeTimer_Tick(object? sender, EventArgs e) => UpdateUptime();

    private void UpdateUptime()
    {
        var elapsed = DateTime.Now - _startedAt;
        uptimeLabel.Text = $"已运行 {elapsed:hh\\:mm\\:ss}";
    }

    /// <summary>
    /// 核心拦截逻辑：只拦截“用户点 ×”这一种关闭原因。
    /// 系统关机、任务管理器结束进程时 CloseReason 不同，必须放行，否则窗口关不掉。
    /// </summary>
    private void TrayForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_exiting || e.CloseReason != CloseReason.UserClosing) return;

        e.Cancel = true;
        HideToTray();
    }

    /// <summary>窗体真正关闭后，隐藏托盘图标并停掉计时器，避免托盘残留。</summary>
    private void TrayForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        uptimeTimer.Stop();
        notifyIcon.Visible = false;
    }
}
