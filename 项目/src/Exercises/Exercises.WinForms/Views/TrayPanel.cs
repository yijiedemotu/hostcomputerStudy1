using Exercises.WinForms.Controls;

namespace Exercises.WinForms.Views;

/// <summary>
/// 第 04 章练习 5：窗口关闭时最小化到托盘。
///
/// 本页只负责“打开/恢复演示窗口”，真正的托盘逻辑在 Controls/TrayDemoForm.cs。
/// 演示窗口用的是“非模态”方式（Show 而不是 ShowDialog）：
/// - ShowDialog 会锁住父窗口，用户必须先关掉它；
/// - Show 打开后，父窗口和演示窗口都能操作，更适合演示托盘行为。
/// </summary>
internal sealed class TrayPanel : UserControl
{
    // 保存演示窗口引用：再次点击“打开”时能恢复到前台，而不是重复创建。
    // 问号表示可能还没有创建（null）。
    private TrayDemoForm? _demoForm;
    private readonly Label _statusLabel;

    public TrayPanel()
    {
        var page = UiTheme.CreatePage(
            "练习 04-5 · 关闭窗口最小化到托盘",
            "用 NotifyIcon 放一个托盘图标；在 FormClosing 里拦截用户点 ×，改为 Hide()；托盘双击时再 Show() 回来。",
            out var content);
        Dock = DockStyle.Fill;
        Controls.Add(page);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = UiTheme.Surface
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            BackColor = UiTheme.Surface,
            Margin = new Padding(0),
            Padding = new Padding(0, 8, 0, 8)
        };
        var openButton = UiTheme.Button("打开演示窗口", primary: true);
        openButton.Click += (_, _) => OpenDemo();
        toolbar.Controls.Add(openButton);

        _statusLabel = new Label
        {
            Text = "尚未打开演示窗口",
            AutoSize = true,
            Font = UiTheme.BodyFont,
            ForeColor = UiTheme.Muted,
            Margin = new Padding(10, 9, 0, 0)
        };
        toolbar.Controls.Add(_statusLabel);
        layout.Controls.Add(toolbar, 0, 0);

        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(248, 250, 252),
            Padding = new Padding(22, 18, 22, 18),
            Margin = new Padding(0)
        };
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(UiTheme.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        card.Controls.Add(new Label
        {
            Text = "实现要点",
            Font = UiTheme.SectionFont,
            ForeColor = UiTheme.Text,
            AutoSize = true,
            Location = new Point(20, 16)
        });

        var points = new[]
        {
            "1. 创建 NotifyIcon，设置 Icon / Text / Visible = true，再挂一个 ContextMenuStrip 作为右键菜单。",
            "2. 在 FormClosing 中判断 e.CloseReason == CloseReason.UserClosing，e.Cancel = true 后调用 Hide()。",
            "3. 托盘图标 DoubleClick → ShowInTaskbar = true + Show() + Activate()，把窗口还原到前台。",
            "4. 托盘菜单提供“退出程序”：先置退出标志，再真正 Close()；OnFormClosed 里 Visible = false 并 Dispose 图标，避免托盘残留。",
            "5. 不要无条件拦截 FormClosing；主窗关闭或系统关机时（CloseReason 不同）必须放行，否则进程退不掉。"
        };

        var y = 54;
        foreach (var point in points)
        {
            var label = new Label
            {
                Text = point,
                Font = UiTheme.BodyFont,
                ForeColor = UiTheme.Text,
                AutoSize = true,
                MaximumSize = new Size(1000, 0),
                Location = new Point(20, y)
            };
            card.Controls.Add(label);
            y += label.PreferredHeight + 10;
        }
        layout.Controls.Add(card, 0, 1);

        layout.Controls.Add(UiTheme.Hint("打开演示窗口后，先点右上角 ×，再双击系统托盘里的图标，观察窗口恢复；最后用托盘右键菜单“退出程序”结束演示窗口。"),
            0, 2);
        content.Controls.Add(layout);
    }

    private void OpenDemo()
    {
        // 已经打开（且没有被释放）：直接恢复并激活，不重复创建窗口。
        if (_demoForm is { IsDisposed: false })
        {
            _demoForm.RestoreWindow();
            _demoForm.Activate();
            _statusLabel.Text = "演示窗口已恢复";
            return;
        }

        // FindForm 找到当前页面所在的主窗体，作为演示窗口的 Owner。
        // 指定 Owner 后，演示窗口会跟随主窗体居中/最小化，也更符合桌面程序习惯。
        var owner = FindForm();

        _demoForm = new TrayDemoForm();

        // 演示窗口真正退出后清空引用，下次点击可以重新创建。
        _demoForm.FormClosed += (_, _) =>
        {
            _demoForm = null;
            _statusLabel.Text = "演示窗口已退出，托盘图标已释放";
        };

        // Show(owner)：以非模态方式显示，不会阻塞主界面。
        _demoForm.Show(owner);
        _statusLabel.Text = "演示窗口已打开 · 点 × 试试隐藏到托盘";
    }

    /// <summary>
    /// 页面销毁时，连同演示窗口一起关掉。
    /// 否则主程序退出后，一个隐藏的托盘窗口可能让进程残留。
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing && _demoForm is { IsDisposed: false })
            _demoForm.ExitWindow();
        base.Dispose(disposing);
    }
}
