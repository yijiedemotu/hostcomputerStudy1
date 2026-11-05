using Answers.WinForms.Controls;

namespace Answers.WinForms.Forms;

partial class TrendHoverForm
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
        statsPanel = new Panel();
        currentTitleLabel = new Label();
        currentValueLabel = new Label();
        timeLabel = new Label();
        statsHintLabel = new Label();
        toolbarPanel = new FlowLayoutPanel();
        btnPause = new Button();
        btnClear = new Button();
        statusLabel = new Label();
        chart = new HoverTrendChart();
        footerLabel = new Label();
        timer = new System.Windows.Forms.Timer(components);
        rootLayout.SuspendLayout();
        headerPanel.SuspendLayout();
        contentPanel.SuspendLayout();
        contentLayout.SuspendLayout();
        statsPanel.SuspendLayout();
        toolbarPanel.SuspendLayout();
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
        rootLayout.Size = new Size(1040, 680);
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
        headerPanel.Size = new Size(992, 86);
        headerPanel.TabIndex = 0;
        // 
        // badgeLabel
        // 
        badgeLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        badgeLabel.BackColor = Color.FromArgb(230, 242, 252);
        badgeLabel.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold);
        badgeLabel.ForeColor = Color.FromArgb(0, 95, 170);
        badgeLabel.Location = new Point(786, 8);
        badgeLabel.Name = "badgeLabel";
        badgeLabel.Size = new Size(206, 30);
        badgeLabel.TabIndex = 2;
        badgeLabel.Text = "OnPaint · OnMouseMove";
        badgeLabel.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // subtitleLabel
        // 
        subtitleLabel.AutoSize = true;
        subtitleLabel.Font = new Font("Microsoft YaHei UI", 9.5F);
        subtitleLabel.ForeColor = Color.FromArgb(100, 116, 139);
        subtitleLabel.Location = new Point(2, 52);
        subtitleLabel.Name = "subtitleLabel";
        subtitleLabel.Size = new Size(646, 19);
        subtitleLabel.TabIndex = 1;
        subtitleLabel.Text = "自绘曲线并重写 OnMouseMove，把鼠标 X 坐标反算成数据点，显示时间与数值。";
        // 
        // titleLabel
        // 
        titleLabel.AutoSize = true;
        titleLabel.Font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold);
        titleLabel.ForeColor = Color.FromArgb(31, 41, 55);
        titleLabel.Location = new Point(0, 8);
        titleLabel.Name = "titleLabel";
        titleLabel.Size = new Size(301, 31);
        titleLabel.TabIndex = 0;
        titleLabel.Text = "第 04 章练习 3 · 曲线悬停";
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
        contentPanel.Size = new Size(992, 524);
        contentPanel.TabIndex = 1;
        // 
        // contentLayout
        // 
        contentLayout.ColumnCount = 1;
        contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        contentLayout.Controls.Add(statsPanel, 0, 0);
        contentLayout.Controls.Add(toolbarPanel, 0, 1);
        contentLayout.Controls.Add(chart, 0, 2);
        contentLayout.Dock = DockStyle.Fill;
        contentLayout.Location = new Point(20, 20);
        contentLayout.Margin = new Padding(0);
        contentLayout.Name = "contentLayout";
        contentLayout.RowCount = 3;
        contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 104F));
        contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
        contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        contentLayout.Size = new Size(952, 484);
        contentLayout.TabIndex = 0;
        // 
        // statsPanel
        // 
        statsPanel.BackColor = Color.FromArgb(248, 250, 252);
        statsPanel.BorderStyle = BorderStyle.FixedSingle;
        statsPanel.Controls.Add(currentTitleLabel);
        statsPanel.Controls.Add(currentValueLabel);
        statsPanel.Controls.Add(timeLabel);
        statsPanel.Controls.Add(statsHintLabel);
        statsPanel.Dock = DockStyle.Fill;
        statsPanel.Location = new Point(0, 0);
        statsPanel.Margin = new Padding(0);
        statsPanel.Name = "statsPanel";
        statsPanel.Size = new Size(952, 104);
        statsPanel.TabIndex = 0;
        // 
        // currentTitleLabel
        // 
        currentTitleLabel.AutoSize = true;
        currentTitleLabel.Font = new Font("Microsoft YaHei UI", 9.5F);
        currentTitleLabel.ForeColor = Color.FromArgb(100, 116, 139);
        currentTitleLabel.Location = new Point(16, 10);
        currentTitleLabel.Name = "currentTitleLabel";
        currentTitleLabel.Size = new Size(68, 19);
        currentTitleLabel.TabIndex = 0;
        currentTitleLabel.Text = "当前温度";
        // 
        // currentValueLabel
        // 
        currentValueLabel.AutoSize = true;
        currentValueLabel.Font = new Font("Microsoft YaHei UI", 24F, FontStyle.Bold);
        currentValueLabel.ForeColor = Color.FromArgb(0, 120, 212);
        currentValueLabel.Location = new Point(14, 32);
        currentValueLabel.Name = "currentValueLabel";
        currentValueLabel.Size = new Size(133, 43);
        currentValueLabel.TabIndex = 1;
        currentValueLabel.Text = "--.- ℃";
        // 
        // timeLabel
        // 
        timeLabel.AutoSize = true;
        timeLabel.Font = new Font("Consolas", 10F);
        timeLabel.ForeColor = Color.FromArgb(31, 41, 55);
        timeLabel.Location = new Point(228, 38);
        timeLabel.Name = "timeLabel";
        timeLabel.Size = new Size(96, 17);
        timeLabel.TabIndex = 2;
        timeLabel.Text = "等待采集";
        // 
        // statsHintLabel
        // 
        statsHintLabel.AutoSize = true;
        statsHintLabel.Font = new Font("Microsoft YaHei UI", 9F);
        statsHintLabel.ForeColor = Color.FromArgb(100, 116, 139);
        statsHintLabel.Location = new Point(228, 68);
        statsHintLabel.Name = "statsHintLabel";
        statsHintLabel.Size = new Size(410, 17);
        statsHintLabel.TabIndex = 3;
        statsHintLabel.Text = "鼠标悬停曲线任意位置可查看历史点，移出图表后提示自动消失。";
        // 
        // toolbarPanel
        // 
        toolbarPanel.Controls.Add(btnPause);
        toolbarPanel.Controls.Add(btnClear);
        toolbarPanel.Controls.Add(statusLabel);
        toolbarPanel.Dock = DockStyle.Fill;
        toolbarPanel.Location = new Point(0, 104);
        toolbarPanel.Margin = new Padding(0);
        toolbarPanel.Name = "toolbarPanel";
        toolbarPanel.Padding = new Padding(0, 8, 0, 8);
        toolbarPanel.Size = new Size(952, 50);
        toolbarPanel.TabIndex = 1;
        toolbarPanel.WrapContents = false;
        // 
        // btnPause
        // 
        btnPause.BackColor = Color.FromArgb(0, 120, 212);
        btnPause.Cursor = Cursors.Hand;
        btnPause.FlatAppearance.BorderSize = 0;
        btnPause.FlatStyle = FlatStyle.Flat;
        btnPause.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold);
        btnPause.ForeColor = Color.White;
        btnPause.Location = new Point(0, 8);
        btnPause.Margin = new Padding(0, 0, 8, 0);
        btnPause.Name = "btnPause";
        btnPause.Size = new Size(96, 34);
        btnPause.TabIndex = 0;
        btnPause.Text = "暂停";
        btnPause.UseVisualStyleBackColor = false;
        btnPause.Click += btnPause_Click;
        // 
        // btnClear
        // 
        btnClear.BackColor = Color.White;
        btnClear.Cursor = Cursors.Hand;
        btnClear.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        btnClear.FlatStyle = FlatStyle.Flat;
        btnClear.Font = new Font("Microsoft YaHei UI", 9.5F);
        btnClear.ForeColor = Color.FromArgb(31, 41, 55);
        btnClear.Location = new Point(104, 8);
        btnClear.Margin = new Padding(0, 0, 8, 0);
        btnClear.Name = "btnClear";
        btnClear.Size = new Size(104, 34);
        btnClear.TabIndex = 1;
        btnClear.Text = "清空曲线";
        btnClear.UseVisualStyleBackColor = false;
        btnClear.Click += btnClear_Click;
        // 
        // statusLabel
        // 
        statusLabel.AutoSize = true;
        statusLabel.Font = new Font("Microsoft YaHei UI", 9.5F);
        statusLabel.ForeColor = Color.FromArgb(100, 116, 139);
        statusLabel.Location = new Point(224, 14);
        statusLabel.Margin = new Padding(8, 6, 0, 0);
        statusLabel.Name = "statusLabel";
        statusLabel.Size = new Size(246, 19);
        statusLabel.TabIndex = 2;
        statusLabel.Text = "采集中 · 500 ms/点 · 保留最近 120 点";
        // 
        // chart
        // 
        chart.BackColor = Color.White;
        chart.Cursor = Cursors.Cross;
        chart.Dock = DockStyle.Fill;
        chart.Location = new Point(0, 154);
        chart.Margin = new Padding(0);
        chart.Name = "chart";
        chart.Size = new Size(952, 330);
        chart.TabIndex = 2;
        // 
        // footerLabel
        // 
        footerLabel.Dock = DockStyle.Fill;
        footerLabel.Font = new Font("Microsoft YaHei UI", 9F);
        footerLabel.ForeColor = Color.FromArgb(100, 116, 139);
        footerLabel.Location = new Point(24, 632);
        footerLabel.Margin = new Padding(2, 4, 2, 0);
        footerLabel.Name = "footerLabel";
        footerLabel.Size = new Size(990, 32);
        footerLabel.TabIndex = 2;
        footerLabel.Text = "设计器提示：chart 是自定义控件 HoverTrendChart，属性窗口可改 BackColor/Dock，运行时才绘制曲线。";
        footerLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // timer
        // 
        timer.Interval = 500;
        timer.Tick += timer_Tick;
        // 
        // TrendHoverForm
        // 
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(244, 246, 250);
        ClientSize = new Size(1040, 680);
        Controls.Add(rootLayout);
        Font = new Font("Microsoft YaHei UI", 9.5F);
        MinimumSize = new Size(900, 600);
        Name = "TrendHoverForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "第 04 章练习 3 · 曲线悬停";
        rootLayout.ResumeLayout(false);
        headerPanel.ResumeLayout(false);
        headerPanel.PerformLayout();
        contentPanel.ResumeLayout(false);
        contentLayout.ResumeLayout(false);
        statsPanel.ResumeLayout(false);
        statsPanel.PerformLayout();
        toolbarPanel.ResumeLayout(false);
        toolbarPanel.PerformLayout();
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
    private Panel statsPanel;
    private Label currentTitleLabel;
    private Label currentValueLabel;
    private Label timeLabel;
    private Label statsHintLabel;
    private FlowLayoutPanel toolbarPanel;
    private Button btnPause;
    private Button btnClear;
    private Label statusLabel;
    private HoverTrendChart chart;
    private Label footerLabel;
    private System.Windows.Forms.Timer timer;
}
