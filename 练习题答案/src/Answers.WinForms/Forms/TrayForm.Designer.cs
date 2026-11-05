namespace Answers.WinForms.Forms;

partial class TrayForm
{
    private System.ComponentModel.IContainer components = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        rootLayout = new TableLayoutPanel();
        headerPanel = new Panel();
        badgeLabel = new Label();
        subtitleLabel = new Label();
        titleLabel = new Label();
        contentPanel = new Panel();
        contentLayout = new TableLayoutPanel();
        introLabel = new Label();
        stepsPanel = new Panel();
        stepsTitleLabel = new Label();
        stepsLabel = new Label();
        actionPanel = new Panel();
        stateTitleLabel = new Label();
        stateLabel = new Label();
        uptimeLabel = new Label();
        btnHide = new Button();
        btnExit = new Button();
        footerLabel = new Label();
        notifyIcon = new NotifyIcon(components);
        contextMenu = new ContextMenuStrip(components);
        menuRestore = new ToolStripMenuItem();
        menuExit = new ToolStripMenuItem();
        uptimeTimer = new System.Windows.Forms.Timer(components);
        rootLayout.SuspendLayout();
        headerPanel.SuspendLayout();
        contentPanel.SuspendLayout();
        contentLayout.SuspendLayout();
        stepsPanel.SuspendLayout();
        actionPanel.SuspendLayout();
        contextMenu.SuspendLayout();
        SuspendLayout();
        // 
        // rootLayout
        // 
        rootLayout.BackColor = Color.FromArgb(244, 246, 250);
        rootLayout.ColumnCount = 1;
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rootLayout.Controls.Add(headerPanel, 0, 0);
        rootLayout.Controls.Add(contentPanel, 0, 1);
        rootLayout.Controls.Add(footerLabel, 0, 2);
        rootLayout.Dock = DockStyle.Fill;
        rootLayout.Location = new Point(0, 0);
        rootLayout.Margin = new Padding(0);
        rootLayout.Name = "rootLayout";
        rootLayout.Padding = new Padding(24, 18, 24, 16);
        rootLayout.RowCount = 3;
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 86F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        rootLayout.Size = new Size(1000, 620);
        rootLayout.TabIndex = 0;
        // 
        // headerPanel
        // 
        headerPanel.Controls.Add(badgeLabel);
        headerPanel.Controls.Add(subtitleLabel);
        headerPanel.Controls.Add(titleLabel);
        headerPanel.Dock = DockStyle.Fill;
        headerPanel.Location = new Point(24, 18);
        headerPanel.Margin = new Padding(0);
        headerPanel.Name = "headerPanel";
        headerPanel.Size = new Size(952, 86);
        headerPanel.TabIndex = 0;
        // 
        // badgeLabel
        // 
        badgeLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        badgeLabel.BackColor = Color.FromArgb(230, 242, 252);
        badgeLabel.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold);
        badgeLabel.ForeColor = Color.FromArgb(0, 95, 170);
        badgeLabel.Location = new Point(742, 8);
        badgeLabel.Name = "badgeLabel";
        badgeLabel.Size = new Size(210, 30);
        badgeLabel.TabIndex = 2;
        badgeLabel.Text = "NotifyIcon · FormClosing";
        badgeLabel.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // subtitleLabel
        // 
        subtitleLabel.AutoSize = true;
        subtitleLabel.Font = new Font("Microsoft YaHei UI", 9.5F);
        subtitleLabel.ForeColor = Color.FromArgb(100, 116, 139);
        subtitleLabel.Location = new Point(2, 52);
        subtitleLabel.Name = "subtitleLabel";
        subtitleLabel.Size = new Size(644, 19);
        subtitleLabel.TabIndex = 1;
        subtitleLabel.Text = "点 × 不退出，而是隐藏到系统托盘；双击托盘图标恢复，右键菜单真正退出。";
        // 
        // titleLabel
        // 
        titleLabel.AutoSize = true;
        titleLabel.Font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold);
        titleLabel.ForeColor = Color.FromArgb(31, 41, 55);
        titleLabel.Location = new Point(0, 8);
        titleLabel.Name = "titleLabel";
        titleLabel.Size = new Size(303, 31);
        titleLabel.TabIndex = 0;
        titleLabel.Text = "第 04 章练习 5 · 托盘最小化";
        // 
        // contentPanel
        // 
        contentPanel.BackColor = Color.White;
        contentPanel.Controls.Add(contentLayout);
        contentPanel.Dock = DockStyle.Fill;
        contentPanel.Location = new Point(24, 104);
        contentPanel.Margin = new Padding(0);
        contentPanel.Name = "contentPanel";
        contentPanel.Padding = new Padding(20);
        contentPanel.Size = new Size(952, 464);
        contentPanel.TabIndex = 1;
        // 
        // contentLayout
        // 
        contentLayout.ColumnCount = 1;
        contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        contentLayout.Controls.Add(introLabel, 0, 0);
        contentLayout.Controls.Add(stepsPanel, 0, 1);
        contentLayout.Controls.Add(actionPanel, 0, 2);
        contentLayout.Dock = DockStyle.Fill;
        contentLayout.Location = new Point(20, 20);
        contentLayout.Margin = new Padding(0);
        contentLayout.Name = "contentLayout";
        contentLayout.RowCount = 3;
        contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
        contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 150F));
        contentLayout.Size = new Size(912, 424);
        contentLayout.TabIndex = 0;
        // 
        // introLabel
        // 
        introLabel.Dock = DockStyle.Fill;
        introLabel.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
        introLabel.ForeColor = Color.FromArgb(31, 41, 55);
        introLabel.Location = new Point(0, 0);
        introLabel.Margin = new Padding(0);
        introLabel.Name = "introLabel";
        introLabel.Size = new Size(912, 50);
        introLabel.TabIndex = 0;
        introLabel.Text = "关闭窗口 ≠ 退出程序：拦截关闭、隐藏窗口、保留托盘图标，再想办法恢复。";
        introLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // stepsPanel
        // 
        stepsPanel.BackColor = Color.FromArgb(248, 250, 252);
        stepsPanel.BorderStyle = BorderStyle.FixedSingle;
        stepsPanel.Controls.Add(stepsTitleLabel);
        stepsPanel.Controls.Add(stepsLabel);
        stepsPanel.Dock = DockStyle.Fill;
        stepsPanel.Location = new Point(0, 50);
        stepsPanel.Margin = new Padding(0);
        stepsPanel.Name = "stepsPanel";
        stepsPanel.Size = new Size(912, 224);
        stepsPanel.TabIndex = 1;
        // 
        // stepsTitleLabel
        // 
        stepsTitleLabel.AutoSize = true;
        stepsTitleLabel.Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold);
        stepsTitleLabel.ForeColor = Color.FromArgb(31, 41, 55);
        stepsTitleLabel.Location = new Point(18, 14);
        stepsTitleLabel.Name = "stepsTitleLabel";
        stepsTitleLabel.Size = new Size(128, 19);
        stepsTitleLabel.TabIndex = 0;
        stepsTitleLabel.Text = "实现要点（5 步）";
        // 
        // stepsLabel
        // 
        stepsLabel.AutoSize = true;
        stepsLabel.Font = new Font("Microsoft YaHei UI", 9.5F);
        stepsLabel.ForeColor = Color.FromArgb(31, 41, 55);
        stepsLabel.Location = new Point(18, 46);
        stepsLabel.MaximumSize = new Size(860, 0);
        stepsLabel.Name = "stepsLabel";
        stepsLabel.Size = new Size(825, 95);
        stepsLabel.TabIndex = 1;
        stepsLabel.Text = "1. 创建 NotifyIcon，设置 Icon / Text / Visible = true，并挂上 ContextMenuStrip 作为右键菜单。\r\n2. 在 FormClosing 中判断 e.CloseReason == CloseReason.UserClosing，取消关闭并 Hide()。\r\n3. 托盘图标 DoubleClick → ShowInTaskbar = true + Show() + Activate()，把窗口还原到前台。\r\n4. 托盘菜单提供“退出程序”：先置退出标志，再真正 Close()，并在关闭后隐藏图标。\r\n5. 不要无条件拦截 FormClosing；主窗关闭或系统关机时必须放行，否则进程退不掉。";
        // 
        // actionPanel
        // 
        actionPanel.Controls.Add(stateTitleLabel);
        actionPanel.Controls.Add(stateLabel);
        actionPanel.Controls.Add(uptimeLabel);
        actionPanel.Controls.Add(btnHide);
        actionPanel.Controls.Add(btnExit);
        actionPanel.Dock = DockStyle.Fill;
        actionPanel.Location = new Point(0, 274);
        actionPanel.Margin = new Padding(0);
        actionPanel.Name = "actionPanel";
        actionPanel.Size = new Size(912, 150);
        actionPanel.TabIndex = 2;
        // 
        // stateTitleLabel
        // 
        stateTitleLabel.AutoSize = true;
        stateTitleLabel.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold);
        stateTitleLabel.ForeColor = Color.FromArgb(100, 116, 139);
        stateTitleLabel.Location = new Point(0, 12);
        stateTitleLabel.Name = "stateTitleLabel";
        stateTitleLabel.Size = new Size(68, 19);
        stateTitleLabel.TabIndex = 0;
        stateTitleLabel.Text = "当前状态";
        // 
        // stateLabel
        // 
        stateLabel.AutoSize = true;
        stateLabel.Font = new Font("Microsoft YaHei UI", 13F, FontStyle.Bold);
        stateLabel.ForeColor = Color.FromArgb(31, 41, 55);
        stateLabel.Location = new Point(0, 38);
        stateLabel.MaximumSize = new Size(560, 0);
        stateLabel.Name = "stateLabel";
        stateLabel.Size = new Size(182, 23);
        stateLabel.TabIndex = 1;
        stateLabel.Text = "窗口显示中 · 点 × 试试";
        // 
        // uptimeLabel
        // 
        uptimeLabel.AutoSize = true;
        uptimeLabel.Font = new Font("Consolas", 10F);
        uptimeLabel.ForeColor = Color.FromArgb(100, 116, 139);
        uptimeLabel.Location = new Point(0, 76);
        uptimeLabel.Name = "uptimeLabel";
        uptimeLabel.Size = new Size(96, 17);
        uptimeLabel.TabIndex = 2;
        uptimeLabel.Text = "已运行 00:00:00";
        // 
        // btnHide
        // 
        btnHide.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnHide.BackColor = Color.FromArgb(0, 120, 212);
        btnHide.Cursor = Cursors.Hand;
        btnHide.FlatAppearance.BorderSize = 0;
        btnHide.FlatStyle = FlatStyle.Flat;
        btnHide.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold);
        btnHide.ForeColor = Color.White;
        btnHide.Location = new Point(646, 32);
        btnHide.Name = "btnHide";
        btnHide.Size = new Size(140, 38);
        btnHide.TabIndex = 3;
        btnHide.Text = "隐藏到托盘";
        btnHide.UseVisualStyleBackColor = false;
        btnHide.Click += btnHide_Click;
        // 
        // btnExit
        // 
        btnExit.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnExit.BackColor = Color.White;
        btnExit.Cursor = Cursors.Hand;
        btnExit.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        btnExit.FlatStyle = FlatStyle.Flat;
        btnExit.Font = new Font("Microsoft YaHei UI", 9.5F);
        btnExit.ForeColor = Color.FromArgb(31, 41, 55);
        btnExit.Location = new Point(796, 32);
        btnExit.Name = "btnExit";
        btnExit.Size = new Size(116, 38);
        btnExit.TabIndex = 4;
        btnExit.Text = "退出程序";
        btnExit.UseVisualStyleBackColor = false;
        btnExit.Click += btnExit_Click;
        // 
        // footerLabel
        // 
        footerLabel.Dock = DockStyle.Fill;
        footerLabel.Font = new Font("Microsoft YaHei UI", 9F);
        footerLabel.ForeColor = Color.FromArgb(100, 116, 139);
        footerLabel.Location = new Point(24, 572);
        footerLabel.Margin = new Padding(2, 4, 2, 0);
        footerLabel.Name = "footerLabel";
        footerLabel.Size = new Size(950, 32);
        footerLabel.TabIndex = 2;
        footerLabel.Text = "操作顺序：点右上角 × 或“隐藏到托盘” → 双击托盘图标恢复 → 右键托盘菜单（或本页按钮）“退出程序”。";
        footerLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // notifyIcon
        // 
        notifyIcon.ContextMenuStrip = contextMenu;
        notifyIcon.Text = "上位机练习 · 托盘演示";
        notifyIcon.DoubleClick += notifyIcon_DoubleClick;
        // 
        // contextMenu
        // 
        contextMenu.Items.AddRange(new ToolStripItem[] { menuRestore, menuExit });
        contextMenu.Name = "contextMenu";
        contextMenu.Size = new Size(125, 48);
        // 
        // menuRestore
        // 
        menuRestore.Name = "menuRestore";
        menuRestore.Size = new Size(124, 22);
        menuRestore.Text = "恢复窗口";
        menuRestore.Click += menuRestore_Click;
        // 
        // menuExit
        // 
        menuExit.Name = "menuExit";
        menuExit.Size = new Size(124, 22);
        menuExit.Text = "退出程序";
        menuExit.Click += menuExit_Click;
        // 
        // uptimeTimer
        // 
        uptimeTimer.Interval = 1000;
        uptimeTimer.Tick += uptimeTimer_Tick;
        // 
        // TrayForm
        // 
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(244, 246, 250);
        ClientSize = new Size(1000, 620);
        Controls.Add(rootLayout);
        Font = new Font("Microsoft YaHei UI", 9.5F);
        MaximizeBox = false;
        MinimumSize = new Size(900, 580);
        Name = "TrayForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "第 04 章练习 5 · 托盘最小化";
        FormClosing += TrayForm_FormClosing;
        FormClosed += TrayForm_FormClosed;
        rootLayout.ResumeLayout(false);
        headerPanel.ResumeLayout(false);
        headerPanel.PerformLayout();
        contentPanel.ResumeLayout(false);
        contentLayout.ResumeLayout(false);
        stepsPanel.ResumeLayout(false);
        stepsPanel.PerformLayout();
        actionPanel.ResumeLayout(false);
        actionPanel.PerformLayout();
        contextMenu.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel rootLayout;
    private Panel headerPanel;
    private Label badgeLabel;
    private Label subtitleLabel;
    private Label titleLabel;
    private Panel contentPanel;
    private TableLayoutPanel contentLayout;
    private Label introLabel;
    private Panel stepsPanel;
    private Label stepsTitleLabel;
    private Label stepsLabel;
    private Panel actionPanel;
    private Label stateTitleLabel;
    private Label stateLabel;
    private Label uptimeLabel;
    private Button btnHide;
    private Button btnExit;
    private Label footerLabel;
    private NotifyIcon notifyIcon;
    private ContextMenuStrip contextMenu;
    private ToolStripMenuItem menuRestore;
    private ToolStripMenuItem menuExit;
    private System.Windows.Forms.Timer uptimeTimer;
}
