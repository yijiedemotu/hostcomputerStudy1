using Answers.WinForms.Forms;

namespace Answers.WinForms;

/// <summary>
/// 主窗口：6 个练习的入口。
///
/// 这个窗口和所有练习窗体都有完整的 .Designer.cs，
/// 在 Visual Studio / Rider 里双击 MainForm.cs 或 MainForm.Designer.cs
/// 就能在设计器中看到下面这套布局。
/// </summary>
public partial class MainForm : Form
{
    private TrayForm? _trayForm;

    public MainForm()
    {
        InitializeComponent();
    }

    private void btnFirstTask_Click(object? sender, EventArgs e) => OpenDialog(new FirstTaskForm());

    private void btnSendHistory_Click(object? sender, EventArgs e) => OpenDialog(new SendHistoryForm());

    private void btnAlarmFilter_Click(object? sender, EventArgs e) => OpenDialog(new AlarmFilterForm());

    private void btnTrendHover_Click(object? sender, EventArgs e) => OpenDialog(new TrendHoverForm());

    private void btnVirtualGrid_Click(object? sender, EventArgs e) => OpenDialog(new VirtualGridForm());

    /// <summary>
    /// 托盘练习必须用非模态 Show：窗体隐藏到托盘时并没有关闭，
    /// ShowDialog 会一直锁住主窗口，用户就没办法继续操作主界面了。
    /// </summary>
    private void btnTray_Click(object? sender, EventArgs e)
    {
        if (_trayForm is { IsDisposed: false })
        {
            _trayForm.RestoreWindow();
            _trayForm.Activate();
            return;
        }

        _trayForm = new TrayForm();
        _trayForm.FormClosed += (_, _) => _trayForm = null;
        _trayForm.Show(this);
    }

    private void OpenDialog(Form form)
    {
        using (form)
        {
            form.ShowDialog(this);
        }
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        // 主窗口退出时把隐藏的托盘窗口也真正关掉，避免进程残留。
        if (_trayForm is { IsDisposed: false })
            _trayForm.ExitWindow();
    }

    private void MainForm_Load(object sender, EventArgs e)
    {
        throw new System.NotImplementedException();
    }

    private void titleLabel_Click(object sender, EventArgs e)
    {

    }

    private void subtitleLabel_Click(object sender, EventArgs e)
    {

    }

    private void footerLabel_Click(object sender, EventArgs e)
    {

    }

    private void rootLayout_Paint(object sender, PaintEventArgs e)
    {

    }

    private void headerPanel_Paint(object sender, PaintEventArgs e)
    {

    }

    private void badgeLabel_Click(object sender, EventArgs e)
    {

    }
}
