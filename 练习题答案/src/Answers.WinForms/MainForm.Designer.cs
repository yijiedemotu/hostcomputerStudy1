namespace Answers.WinForms;

partial class MainForm
{
    /// <summary>
    /// 设计器生成代码：所有控件在这里创建并设置位置、大小、颜色、字体。
    /// 这个文件不要手工删除，否则设计器会重新生成它。
    /// </summary>
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

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        rootLayout = new TableLayoutPanel();
        headerPanel = new Panel();
        badgeLabel = new Label();
        subtitleLabel = new Label();
        titleLabel = new Label();
        cardsPanel = new TableLayoutPanel();
        btnFirstTask = new Button();
        btnSendHistory = new Button();
        btnAlarmFilter = new Button();
        btnTrendHover = new Button();
        btnVirtualGrid = new Button();
        btnTray = new Button();
        footerLabel = new Label();
        rootLayout.SuspendLayout();
        headerPanel.SuspendLayout();
        cardsPanel.SuspendLayout();
        SuspendLayout();
        // 
        // rootLayout
        // 
        rootLayout.BackColor = Color.FromArgb(244, 246, 250);
        rootLayout.ColumnCount = 1;
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rootLayout.Controls.Add(headerPanel, 0, 0);
        rootLayout.Controls.Add(cardsPanel, 0, 1);
        rootLayout.Controls.Add(footerLabel, 0, 2);
        rootLayout.Dock = DockStyle.Fill;
        rootLayout.Location = new Point(0, 0);
        rootLayout.Margin = new Padding(0, 0, 0, 23);
        rootLayout.Name = "rootLayout";
        rootLayout.RowCount = 3;
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 77F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
        rootLayout.Size = new Size(713, 493);
        rootLayout.TabIndex = 0;
        rootLayout.Paint += rootLayout_Paint;
        // 
        // headerPanel
        // 
        headerPanel.BackColor = Color.White;
        headerPanel.Controls.Add(badgeLabel);
        headerPanel.Controls.Add(subtitleLabel);
        headerPanel.Controls.Add(titleLabel);
        headerPanel.Dock = DockStyle.Fill;
        headerPanel.Location = new Point(0, 0);
        headerPanel.Margin = new Padding(0);
        headerPanel.Name = "headerPanel";
        headerPanel.Size = new Size(713, 77);
        headerPanel.TabIndex = 0;
        headerPanel.Paint += headerPanel_Paint;
        // 
        // badgeLabel
        // 
        badgeLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        badgeLabel.BackColor = Color.FromArgb(230, 242, 252);
        badgeLabel.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 134);
        badgeLabel.ForeColor = Color.FromArgb(0, 95, 170);
        badgeLabel.Location = new Point(595, 4);
        badgeLabel.Margin = new Padding(0, 0, 0, 0);
        badgeLabel.Name = "badgeLabel";
        badgeLabel.Size = new Size(89, 30);
        badgeLabel.TabIndex = 2;
        badgeLabel.Text = "第 01 / 04 章 · 6 个练习";
        badgeLabel.TextAlign = ContentAlignment.MiddleCenter;
        badgeLabel.Click += badgeLabel_Click;
        // 
        // subtitleLabel
        // 
        subtitleLabel.AutoSize = true;
        subtitleLabel.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
        subtitleLabel.ForeColor = Color.FromArgb(100, 116, 139);
        subtitleLabel.Location = new Point(12, 34);
        subtitleLabel.Margin = new Padding(0, 0, 0, 0);
        subtitleLabel.Name = "subtitleLabel";
        subtitleLabel.Size = new Size(672, 24);
        subtitleLabel.TabIndex = 1;
        subtitleLabel.Text = "每道题一个独立 Form，布局全部写在 Designer.cs 中，设计器可直接查看和修改。";
        subtitleLabel.Click += subtitleLabel_Click;
        // 
        // titleLabel
        // 
        titleLabel.AutoSize = true;
        titleLabel.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 134);
        titleLabel.ForeColor = Color.FromArgb(31, 41, 55);
        titleLabel.Location = new Point(9, 9);
        titleLabel.Margin = new Padding(0, 0, 0, 0);
        titleLabel.Name = "titleLabel";
        titleLabel.Size = new Size(218, 25);
        titleLabel.TabIndex = 0;
        titleLabel.Text = "WinForms 原生窗体练习";
        titleLabel.Click += titleLabel_Click;
        // 
        // cardsPanel
        // 
        cardsPanel.BackColor = Color.FromArgb(244, 246, 250);
        cardsPanel.ColumnCount = 2;
        cardsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44.8586121F));
        cardsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55.1413879F));
        cardsPanel.Controls.Add(btnFirstTask, 0, 0);
        cardsPanel.Controls.Add(btnSendHistory, 1, 0);
        cardsPanel.Controls.Add(btnAlarmFilter, 0, 1);
        cardsPanel.Controls.Add(btnTrendHover, 1, 1);
        cardsPanel.Controls.Add(btnVirtualGrid, 0, 2);
        cardsPanel.Controls.Add(btnTray, 1, 2);
        cardsPanel.Dock = DockStyle.Fill;
        cardsPanel.Location = new Point(3, 78);
        cardsPanel.Margin = new Padding(3, 1, 3, 0);
        cardsPanel.Name = "cardsPanel";
        cardsPanel.RowCount = 3;
        cardsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33333F));
        cardsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33333F));
        cardsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33333F));
        cardsPanel.Size = new Size(707, 353);
        cardsPanel.TabIndex = 1;
        // 
        // btnFirstTask
        // 
        btnFirstTask.BackColor = Color.White;
        btnFirstTask.Cursor = Cursors.Hand;
        btnFirstTask.Dock = DockStyle.Fill;
        btnFirstTask.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        btnFirstTask.FlatStyle = FlatStyle.Flat;
        btnFirstTask.Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
        btnFirstTask.ForeColor = Color.FromArgb(31, 41, 55);
        btnFirstTask.Location = new Point(0, 0);
        btnFirstTask.Margin = new Padding(0, 0, 1, 1);
        btnFirstTask.Name = "btnFirstTask";
        btnFirstTask.Padding = new Padding(4, 0, 2, 0);
        btnFirstTask.Size = new Size(316, 116);
        btnFirstTask.TabIndex = 0;
        btnFirstTask.Text = "01   第一个小任务\r\nTextBox + Button + Label + CheckBox 的 5 步练习";
        btnFirstTask.TextAlign = ContentAlignment.MiddleLeft;
        btnFirstTask.UseVisualStyleBackColor = false;
        btnFirstTask.Click += btnFirstTask_Click;
        // 
        // btnSendHistory
        // 
        btnSendHistory.BackColor = Color.White;
        btnSendHistory.Cursor = Cursors.Hand;
        btnSendHistory.Dock = DockStyle.Fill;
        btnSendHistory.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        btnSendHistory.FlatStyle = FlatStyle.Flat;
        btnSendHistory.Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
        btnSendHistory.ForeColor = Color.FromArgb(31, 41, 55);
        btnSendHistory.Location = new Point(318, 0);
        btnSendHistory.Margin = new Padding(1, 0, 0, 1);
        btnSendHistory.Name = "btnSendHistory";
        btnSendHistory.Padding = new Padding(4, 0, 2, 0);
        btnSendHistory.Size = new Size(389, 116);
        btnSendHistory.TabIndex = 1;
        btnSendHistory.Text = "04-1   发送历史\r\n最近 10 条 + JSON 持久化 + 双击回填";
        btnSendHistory.TextAlign = ContentAlignment.MiddleLeft;
        btnSendHistory.UseVisualStyleBackColor = false;
        btnSendHistory.Click += btnSendHistory_Click;
        // 
        // btnAlarmFilter
        // 
        btnAlarmFilter.BackColor = Color.White;
        btnAlarmFilter.Cursor = Cursors.Hand;
        btnAlarmFilter.Dock = DockStyle.Fill;
        btnAlarmFilter.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        btnAlarmFilter.FlatStyle = FlatStyle.Flat;
        btnAlarmFilter.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 134);
        btnAlarmFilter.ForeColor = Color.FromArgb(31, 41, 55);
        btnAlarmFilter.Location = new Point(0, 117);
        btnAlarmFilter.Margin = new Padding(0, 0, 1, 1);
        btnAlarmFilter.Name = "btnAlarmFilter";
        btnAlarmFilter.Padding = new Padding(4, 0, 2, 0);
        btnAlarmFilter.Size = new Size(316, 116);
        btnAlarmFilter.TabIndex = 2;
        btnAlarmFilter.Text = "04-2   报警筛选\r\n全量数据不变，按状态切换显示子集";
        btnAlarmFilter.TextAlign = ContentAlignment.MiddleLeft;
        btnAlarmFilter.UseVisualStyleBackColor = false;
        btnAlarmFilter.Click += btnAlarmFilter_Click;
        // 
        // btnTrendHover
        // 
        btnTrendHover.BackColor = Color.White;
        btnTrendHover.Cursor = Cursors.Hand;
        btnTrendHover.Dock = DockStyle.Fill;
        btnTrendHover.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        btnTrendHover.FlatStyle = FlatStyle.Flat;
        btnTrendHover.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 134);
        btnTrendHover.ForeColor = Color.FromArgb(31, 41, 55);
        btnTrendHover.Location = new Point(318, 117);
        btnTrendHover.Margin = new Padding(1, 0, 0, 1);
        btnTrendHover.Name = "btnTrendHover";
        btnTrendHover.Padding = new Padding(4, 0, 2, 0);
        btnTrendHover.Size = new Size(389, 116);
        btnTrendHover.TabIndex = 3;
        btnTrendHover.Text = "04-3   曲线悬停\r\n自绘 GDI+ 趋势图 + 十字光标提示";
        btnTrendHover.TextAlign = ContentAlignment.MiddleLeft;
        btnTrendHover.UseVisualStyleBackColor = false;
        btnTrendHover.Click += btnTrendHover_Click;
        // 
        // btnVirtualGrid
        // 
        btnVirtualGrid.BackColor = Color.White;
        btnVirtualGrid.Cursor = Cursors.Hand;
        btnVirtualGrid.Dock = DockStyle.Fill;
        btnVirtualGrid.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        btnVirtualGrid.FlatStyle = FlatStyle.Flat;
        btnVirtualGrid.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 134);
        btnVirtualGrid.ForeColor = Color.FromArgb(31, 41, 55);
        btnVirtualGrid.Location = new Point(0, 234);
        btnVirtualGrid.Margin = new Padding(0, 0, 1, 23);
        btnVirtualGrid.Name = "btnVirtualGrid";
        btnVirtualGrid.Padding = new Padding(4, 0, 2, 5);
        btnVirtualGrid.Size = new Size(316, 96);
        btnVirtualGrid.TabIndex = 4;
        btnVirtualGrid.Text = "04-4   虚拟表格\r\nVirtualMode 加载 100,000 行历史数据";
        btnVirtualGrid.TextAlign = ContentAlignment.MiddleLeft;
        btnVirtualGrid.UseVisualStyleBackColor = false;
        btnVirtualGrid.Click += btnVirtualGrid_Click;
        // 
        // btnTray
        // 
        btnTray.BackColor = Color.White;
        btnTray.Cursor = Cursors.Hand;
        btnTray.Dock = DockStyle.Fill;
        btnTray.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        btnTray.FlatStyle = FlatStyle.Flat;
        btnTray.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 134);
        btnTray.ForeColor = Color.FromArgb(31, 41, 55);
        btnTray.Location = new Point(318, 234);
        btnTray.Margin = new Padding(1, 0, 0, 23);
        btnTray.Name = "btnTray";
        btnTray.Padding = new Padding(4, 0, 2, 0);
        btnTray.Size = new Size(389, 96);
        btnTray.TabIndex = 5;
        btnTray.Text = "04-5   托盘最小化\r\nFormClosing 拦截关闭 + NotifyIcon 恢复";
        btnTray.TextAlign = ContentAlignment.MiddleLeft;
        btnTray.UseVisualStyleBackColor = false;
        btnTray.Click += btnTray_Click;
        // 
        // footerLabel
        // 
        footerLabel.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 134);
        footerLabel.ForeColor = Color.FromArgb(100, 116, 139);
        footerLabel.Location = new Point(4, 432);
        footerLabel.Margin = new Padding(4, 1, 4, 0);
        footerLabel.Name = "footerLabel";
        footerLabel.Size = new Size(596, 58);
        footerLabel.TabIndex = 2;
        footerLabel.Text = "设计器查看方法：在解决方案资源管理器中展开 Form，双击 .Designer.cs 或按 F7 / Shift+F7 切换代码与设计视图。";
        footerLabel.TextAlign = ContentAlignment.MiddleLeft;
        footerLabel.Click += footerLabel_Click;
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(2F, 5F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(244, 246, 250);
        ClientSize = new Size(713, 493);
        Controls.Add(rootLayout);
        Font = new Font("Microsoft YaHei UI", 1.8765434F);
        Margin = new Padding(1, 1, 1, 1);
        MinimumSize = new Size(191, 161);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "WinForms 练习答案 · 原生窗体";
        FormClosing += MainForm_FormClosing;
        Load += MainForm_Load;
        rootLayout.ResumeLayout(false);
        headerPanel.ResumeLayout(false);
        headerPanel.PerformLayout();
        cardsPanel.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.TableLayoutPanel rootLayout;
    private System.Windows.Forms.Panel headerPanel;
    private System.Windows.Forms.Label badgeLabel;
    private Label subtitleLabel;
    private Label titleLabel;
    private System.Windows.Forms.TableLayoutPanel cardsPanel;
    private System.Windows.Forms.Button btnFirstTask;
    private System.Windows.Forms.Button btnSendHistory;
    private System.Windows.Forms.Button btnAlarmFilter;
    private System.Windows.Forms.Button btnTrendHover;
    private System.Windows.Forms.Button btnVirtualGrid;
    private System.Windows.Forms.Button btnTray;
    private System.Windows.Forms.Label footerLabel;
}
