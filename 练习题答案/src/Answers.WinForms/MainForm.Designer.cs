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
        rootLayout = new System.Windows.Forms.TableLayoutPanel();
        headerPanel = new System.Windows.Forms.Panel();
        badgeLabel = new System.Windows.Forms.Label();
        subtitleLabel = new System.Windows.Forms.Label();
        titleLabel = new System.Windows.Forms.Label();
        cardsPanel = new System.Windows.Forms.TableLayoutPanel();
        btnFirstTask = new System.Windows.Forms.Button();
        btnSendHistory = new System.Windows.Forms.Button();
        btnAlarmFilter = new System.Windows.Forms.Button();
        btnTrendHover = new System.Windows.Forms.Button();
        btnVirtualGrid = new System.Windows.Forms.Button();
        btnTray = new System.Windows.Forms.Button();
        footerLabel = new System.Windows.Forms.Label();
        rootLayout.SuspendLayout();
        headerPanel.SuspendLayout();
        cardsPanel.SuspendLayout();
        SuspendLayout();
        // 
        // rootLayout
        // 
        rootLayout.BackColor = System.Drawing.Color.FromArgb(((int)((byte)244)), ((int)((byte)246)), ((int)((byte)250)));
        rootLayout.ColumnCount = 1;
        rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        rootLayout.Controls.Add(headerPanel, 0, 0);
        rootLayout.Controls.Add(cardsPanel, 0, 1);
        rootLayout.Controls.Add(footerLabel, 0, 2);
        rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
        rootLayout.Location = new System.Drawing.Point(0, 0);
        rootLayout.Margin = new System.Windows.Forms.Padding(0);
        rootLayout.Name = "rootLayout";
        rootLayout.RowCount = 3;
        rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
        rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
        rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 17F));
        rootLayout.Size = new System.Drawing.Size(514, 513);
        rootLayout.TabIndex = 0;
        // 
        // headerPanel
        // 
        headerPanel.BackColor = System.Drawing.Color.White;
        headerPanel.Controls.Add(badgeLabel);
        headerPanel.Controls.Add(subtitleLabel);
        headerPanel.Controls.Add(titleLabel);
        headerPanel.Dock = System.Windows.Forms.DockStyle.Fill;
        headerPanel.Location = new System.Drawing.Point(0, 0);
        headerPanel.Margin = new System.Windows.Forms.Padding(0);
        headerPanel.Name = "headerPanel";
        headerPanel.Size = new System.Drawing.Size(514, 50);
        headerPanel.TabIndex = 0;
        // 
        // badgeLabel
        // 
        badgeLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right));
        badgeLabel.BackColor = System.Drawing.Color.FromArgb(((int)((byte)230)), ((int)((byte)242)), ((int)((byte)252)));
        badgeLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 4.2222223F, System.Drawing.FontStyle.Bold);
        badgeLabel.ForeColor = System.Drawing.Color.FromArgb(((int)((byte)0)), ((int)((byte)95)), ((int)((byte)170)));
        badgeLabel.Location = new System.Drawing.Point(441, 12);
        badgeLabel.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
        badgeLabel.Name = "badgeLabel";
        badgeLabel.Size = new System.Drawing.Size(62, 12);
        badgeLabel.TabIndex = 2;
        badgeLabel.Text = "第 01 / 04 章 · 6 个练习";
        badgeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        // 
        // subtitleLabel
        // 
        subtitleLabel.AutoSize = true;
        subtitleLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 4.4444447F);
        subtitleLabel.ForeColor = System.Drawing.Color.FromArgb(((int)((byte)100)), ((int)((byte)116)), ((int)((byte)139)));
        subtitleLabel.Location = new System.Drawing.Point(11, 28);
        subtitleLabel.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
        subtitleLabel.Name = "subtitleLabel";
        subtitleLabel.Size = new System.Drawing.Size(339, 14);
        subtitleLabel.TabIndex = 1;
        subtitleLabel.Text = "每道题一个独立 Form，布局全部写在 Designer.cs 中，设计器可直接查看和修改。";
        // 
        // titleLabel
        // 
        titleLabel.AutoSize = true;
        titleLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 9.777779F, System.Drawing.FontStyle.Bold);
        titleLabel.ForeColor = System.Drawing.Color.FromArgb(((int)((byte)31)), ((int)((byte)41)), ((int)((byte)55)));
        titleLabel.Location = new System.Drawing.Point(10, 10);
        titleLabel.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
        titleLabel.Name = "titleLabel";
        titleLabel.Size = new System.Drawing.Size(241, 27);
        titleLabel.TabIndex = 0;
        titleLabel.Text = "WinForms 原生窗体练习";
        // 
        // cardsPanel
        // 
        cardsPanel.BackColor = System.Drawing.Color.FromArgb(((int)((byte)244)), ((int)((byte)246)), ((int)((byte)250)));
        cardsPanel.ColumnCount = 2;
        cardsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
        cardsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
        cardsPanel.Controls.Add(btnFirstTask, 0, 0);
        cardsPanel.Controls.Add(btnSendHistory, 1, 0);
        cardsPanel.Controls.Add(btnAlarmFilter, 0, 1);
        cardsPanel.Controls.Add(btnTrendHover, 1, 1);
        cardsPanel.Controls.Add(btnVirtualGrid, 0, 2);
        cardsPanel.Controls.Add(btnTray, 1, 2);
        cardsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
        cardsPanel.Location = new System.Drawing.Point(8, 53);
        cardsPanel.Margin = new System.Windows.Forms.Padding(8, 3, 8, 3);
        cardsPanel.Name = "cardsPanel";
        cardsPanel.RowCount = 3;
        cardsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
        cardsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
        cardsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
        cardsPanel.Size = new System.Drawing.Size(498, 440);
        cardsPanel.TabIndex = 1;
        // 
        // btnFirstTask
        // 
        btnFirstTask.BackColor = System.Drawing.Color.White;
        btnFirstTask.Cursor = System.Windows.Forms.Cursors.Hand;
        btnFirstTask.Dock = System.Windows.Forms.DockStyle.Fill;
        btnFirstTask.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)((byte)226)), ((int)((byte)232)), ((int)((byte)240)));
        btnFirstTask.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        btnFirstTask.Font = new System.Drawing.Font("Microsoft YaHei UI", 5.1111116F);
        btnFirstTask.ForeColor = System.Drawing.Color.FromArgb(((int)((byte)31)), ((int)((byte)41)), ((int)((byte)55)));
        btnFirstTask.Location = new System.Drawing.Point(0, 0);
        btnFirstTask.Margin = new System.Windows.Forms.Padding(0, 0, 3, 3);
        btnFirstTask.Name = "btnFirstTask";
        btnFirstTask.Padding = new System.Windows.Forms.Padding(9, 0, 4, 0);
        btnFirstTask.Size = new System.Drawing.Size(246, 143);
        btnFirstTask.TabIndex = 0;
        btnFirstTask.Text = "01   第一个小任务\r\nTextBox + Button + Label + CheckBox 的 5 步练习";
        btnFirstTask.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        btnFirstTask.UseVisualStyleBackColor = false;
        btnFirstTask.Click += btnFirstTask_Click;
        // 
        // btnSendHistory
        // 
        btnSendHistory.BackColor = System.Drawing.Color.White;
        btnSendHistory.Cursor = System.Windows.Forms.Cursors.Hand;
        btnSendHistory.Dock = System.Windows.Forms.DockStyle.Fill;
        btnSendHistory.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)((byte)226)), ((int)((byte)232)), ((int)((byte)240)));
        btnSendHistory.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        btnSendHistory.Font = new System.Drawing.Font("Microsoft YaHei UI", 5.1111116F);
        btnSendHistory.ForeColor = System.Drawing.Color.FromArgb(((int)((byte)31)), ((int)((byte)41)), ((int)((byte)55)));
        btnSendHistory.Location = new System.Drawing.Point(252, 0);
        btnSendHistory.Margin = new System.Windows.Forms.Padding(3, 0, 0, 3);
        btnSendHistory.Name = "btnSendHistory";
        btnSendHistory.Padding = new System.Windows.Forms.Padding(9, 0, 4, 0);
        btnSendHistory.Size = new System.Drawing.Size(246, 143);
        btnSendHistory.TabIndex = 1;
        btnSendHistory.Text = "04-1   发送历史\r\n最近 10 条 + JSON 持久化 + 双击回填";
        btnSendHistory.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        btnSendHistory.UseVisualStyleBackColor = false;
        btnSendHistory.Click += btnSendHistory_Click;
        // 
        // btnAlarmFilter
        // 
        btnAlarmFilter.BackColor = System.Drawing.Color.White;
        btnAlarmFilter.Cursor = System.Windows.Forms.Cursors.Hand;
        btnAlarmFilter.Dock = System.Windows.Forms.DockStyle.Fill;
        btnAlarmFilter.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)((byte)226)), ((int)((byte)232)), ((int)((byte)240)));
        btnAlarmFilter.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        btnAlarmFilter.Font = new System.Drawing.Font("Microsoft YaHei UI", 5.1111116F);
        btnAlarmFilter.ForeColor = System.Drawing.Color.FromArgb(((int)((byte)31)), ((int)((byte)41)), ((int)((byte)55)));
        btnAlarmFilter.Location = new System.Drawing.Point(0, 146);
        btnAlarmFilter.Margin = new System.Windows.Forms.Padding(0, 0, 3, 3);
        btnAlarmFilter.Name = "btnAlarmFilter";
        btnAlarmFilter.Padding = new System.Windows.Forms.Padding(9, 0, 4, 0);
        btnAlarmFilter.Size = new System.Drawing.Size(246, 143);
        btnAlarmFilter.TabIndex = 2;
        btnAlarmFilter.Text = "04-2   报警筛选\r\n全量数据不变，按状态切换显示子集";
        btnAlarmFilter.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        btnAlarmFilter.UseVisualStyleBackColor = false;
        btnAlarmFilter.Click += btnAlarmFilter_Click;
        // 
        // btnTrendHover
        // 
        btnTrendHover.BackColor = System.Drawing.Color.White;
        btnTrendHover.Cursor = System.Windows.Forms.Cursors.Hand;
        btnTrendHover.Dock = System.Windows.Forms.DockStyle.Fill;
        btnTrendHover.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)((byte)226)), ((int)((byte)232)), ((int)((byte)240)));
        btnTrendHover.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        btnTrendHover.Font = new System.Drawing.Font("Microsoft YaHei UI", 5.1111116F);
        btnTrendHover.ForeColor = System.Drawing.Color.FromArgb(((int)((byte)31)), ((int)((byte)41)), ((int)((byte)55)));
        btnTrendHover.Location = new System.Drawing.Point(252, 146);
        btnTrendHover.Margin = new System.Windows.Forms.Padding(3, 0, 0, 3);
        btnTrendHover.Name = "btnTrendHover";
        btnTrendHover.Padding = new System.Windows.Forms.Padding(9, 0, 4, 0);
        btnTrendHover.Size = new System.Drawing.Size(246, 143);
        btnTrendHover.TabIndex = 3;
        btnTrendHover.Text = "04-3   曲线悬停\r\n自绘 GDI+ 趋势图 + 十字光标提示";
        btnTrendHover.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        btnTrendHover.UseVisualStyleBackColor = false;
        btnTrendHover.Click += btnTrendHover_Click;
        // 
        // btnVirtualGrid
        // 
        btnVirtualGrid.BackColor = System.Drawing.Color.White;
        btnVirtualGrid.Cursor = System.Windows.Forms.Cursors.Hand;
        btnVirtualGrid.Dock = System.Windows.Forms.DockStyle.Fill;
        btnVirtualGrid.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)((byte)226)), ((int)((byte)232)), ((int)((byte)240)));
        btnVirtualGrid.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        btnVirtualGrid.Font = new System.Drawing.Font("Microsoft YaHei UI", 5.1111116F);
        btnVirtualGrid.ForeColor = System.Drawing.Color.FromArgb(((int)((byte)31)), ((int)((byte)41)), ((int)((byte)55)));
        btnVirtualGrid.Location = new System.Drawing.Point(0, 292);
        btnVirtualGrid.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
        btnVirtualGrid.Name = "btnVirtualGrid";
        btnVirtualGrid.Padding = new System.Windows.Forms.Padding(9, 0, 4, 0);
        btnVirtualGrid.Size = new System.Drawing.Size(246, 148);
        btnVirtualGrid.TabIndex = 4;
        btnVirtualGrid.Text = "04-4   虚拟表格\r\nVirtualMode 加载 100,000 行历史数据";
        btnVirtualGrid.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        btnVirtualGrid.UseVisualStyleBackColor = false;
        btnVirtualGrid.Click += btnVirtualGrid_Click;
        // 
        // btnTray
        // 
        btnTray.BackColor = System.Drawing.Color.White;
        btnTray.Cursor = System.Windows.Forms.Cursors.Hand;
        btnTray.Dock = System.Windows.Forms.DockStyle.Fill;
        btnTray.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)((byte)226)), ((int)((byte)232)), ((int)((byte)240)));
        btnTray.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        btnTray.Font = new System.Drawing.Font("Microsoft YaHei UI", 5.1111116F);
        btnTray.ForeColor = System.Drawing.Color.FromArgb(((int)((byte)31)), ((int)((byte)41)), ((int)((byte)55)));
        btnTray.Location = new System.Drawing.Point(252, 292);
        btnTray.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
        btnTray.Name = "btnTray";
        btnTray.Padding = new System.Windows.Forms.Padding(9, 0, 4, 0);
        btnTray.Size = new System.Drawing.Size(246, 148);
        btnTray.TabIndex = 5;
        btnTray.Text = "04-5   托盘最小化\r\nFormClosing 拦截关闭 + NotifyIcon 恢复";
        btnTray.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        btnTray.UseVisualStyleBackColor = false;
        btnTray.Click += btnTray_Click;
        // 
        // footerLabel
        // 
        footerLabel.Dock = System.Windows.Forms.DockStyle.Fill;
        footerLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 4F);
        footerLabel.ForeColor = System.Drawing.Color.FromArgb(((int)((byte)100)), ((int)((byte)116)), ((int)((byte)139)));
        footerLabel.Location = new System.Drawing.Point(11, 497);
        footerLabel.Margin = new System.Windows.Forms.Padding(11, 1, 11, 0);
        footerLabel.Name = "footerLabel";
        footerLabel.Size = new System.Drawing.Size(492, 16);
        footerLabel.TabIndex = 2;
        footerLabel.Text = "设计器查看方法：在解决方案资源管理器中展开 Form，双击 .Designer.cs 或按 F7 / Shift+F7 切换代码与设计视图。";
        footerLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        // 
        // MainForm
        // 
        AutoScaleDimensions = new System.Drawing.SizeF(5F, 11F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        BackColor = System.Drawing.Color.FromArgb(((int)((byte)244)), ((int)((byte)246)), ((int)((byte)250)));
        ClientSize = new System.Drawing.Size(514, 513);
        Controls.Add(rootLayout);
        Font = new System.Drawing.Font("Microsoft YaHei UI", 4.2222223F);
        Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
        MinimumSize = new System.Drawing.Size(454, 308);
        StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
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
