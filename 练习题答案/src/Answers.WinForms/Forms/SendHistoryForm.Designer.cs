namespace Answers.WinForms.Forms;

partial class SendHistoryForm
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
        rootLayout = new TableLayoutPanel();
        headerPanel = new Panel();
        subtitleLabel = new Label();
        titleLabel = new Label();
        contentPanel = new Panel();
        historyCombo = new ComboBox();
        sendButton = new Button();
        clearButton = new Button();
        historyLabel = new Label();
        historyList = new ListBox();
        statusLabel = new Label();
        fileLabel = new Label();
        hintLabel = new Label();
        footerLabel = new Label();
        rootLayout.SuspendLayout();
        headerPanel.SuspendLayout();
        contentPanel.SuspendLayout();
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
        rootLayout.Size = new Size(1000, 720);
        rootLayout.TabIndex = 0;
        // 
        // headerPanel
        // 
        headerPanel.Controls.Add(subtitleLabel);
        headerPanel.Controls.Add(titleLabel);
        headerPanel.Dock = DockStyle.Fill;
        headerPanel.Location = new Point(24, 18);
        headerPanel.Margin = new Padding(0);
        headerPanel.Name = "headerPanel";
        headerPanel.Size = new Size(952, 86);
        headerPanel.TabIndex = 0;
        // 
        // subtitleLabel
        // 
        subtitleLabel.AutoSize = true;
        subtitleLabel.Font = new Font("Microsoft YaHei UI", 9.5F);
        subtitleLabel.ForeColor = Color.FromArgb(100, 116, 139);
        subtitleLabel.Location = new Point(2, 52);
        subtitleLabel.Name = "subtitleLabel";
        subtitleLabel.Size = new Size(620, 19);
        subtitleLabel.TabIndex = 1;
        subtitleLabel.Text = "最近 10 条发送内容，JSON 持久化到用户目录；重启程序后历史仍在，双击列表可回填。";
        // 
        // titleLabel
        // 
        titleLabel.AutoSize = true;
        titleLabel.Font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold);
        titleLabel.ForeColor = Color.FromArgb(31, 41, 55);
        titleLabel.Location = new Point(0, 8);
        titleLabel.Name = "titleLabel";
        titleLabel.Size = new Size(228, 31);
        titleLabel.TabIndex = 0;
        titleLabel.Text = "第 04 章练习 1 · 发送历史";
        // 
        // contentPanel
        // 
        contentPanel.BackColor = Color.White;
        contentPanel.Controls.Add(historyCombo);
        contentPanel.Controls.Add(sendButton);
        contentPanel.Controls.Add(clearButton);
        contentPanel.Controls.Add(historyLabel);
        contentPanel.Controls.Add(historyList);
        contentPanel.Controls.Add(statusLabel);
        contentPanel.Controls.Add(fileLabel);
        contentPanel.Controls.Add(hintLabel);
        contentPanel.Dock = DockStyle.Fill;
        contentPanel.Location = new Point(24, 104);
        contentPanel.Margin = new Padding(0);
        contentPanel.Name = "contentPanel";
        contentPanel.Padding = new Padding(28);
        contentPanel.Size = new Size(952, 564);
        contentPanel.TabIndex = 1;
        // 
        // historyCombo
        // 
        historyCombo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        historyCombo.DropDownStyle = ComboBoxStyle.DropDown;
        historyCombo.Font = new Font("Consolas", 10.5F);
        historyCombo.FormattingEnabled = true;
        historyCombo.Location = new Point(30, 28);
        historyCombo.Name = "historyCombo";
        historyCombo.Size = new Size(650, 25);
        historyCombo.TabIndex = 0;
        historyCombo.SelectedIndexChanged += historyCombo_SelectedIndexChanged;
        // 
        // sendButton
        // 
        sendButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        sendButton.BackColor = Color.FromArgb(0, 120, 212);
        sendButton.Cursor = Cursors.Hand;
        sendButton.FlatAppearance.BorderSize = 0;
        sendButton.FlatStyle = FlatStyle.Flat;
        sendButton.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold);
        sendButton.ForeColor = Color.White;
        sendButton.Location = new Point(696, 24);
        sendButton.Name = "sendButton";
        sendButton.Size = new Size(112, 36);
        sendButton.TabIndex = 1;
        sendButton.Text = "发送（模拟）";
        sendButton.UseVisualStyleBackColor = false;
        sendButton.Click += sendButton_Click;
        // 
        // clearButton
        // 
        clearButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        clearButton.BackColor = Color.White;
        clearButton.Cursor = Cursors.Hand;
        clearButton.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        clearButton.FlatStyle = FlatStyle.Flat;
        clearButton.Font = new Font("Microsoft YaHei UI", 9.5F);
        clearButton.ForeColor = Color.FromArgb(31, 41, 55);
        clearButton.Location = new Point(818, 24);
        clearButton.Name = "clearButton";
        clearButton.Size = new Size(104, 36);
        clearButton.TabIndex = 2;
        clearButton.Text = "清空历史";
        clearButton.UseVisualStyleBackColor = false;
        clearButton.Click += clearButton_Click;
        // 
        // historyLabel
        // 
        historyLabel.AutoSize = true;
        historyLabel.Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold);
        historyLabel.ForeColor = Color.FromArgb(31, 41, 55);
        historyLabel.Location = new Point(28, 82);
        historyLabel.Name = "historyLabel";
        historyLabel.Size = new Size(314, 19);
        historyLabel.TabIndex = 3;
        historyLabel.Text = "最近 10 条（双击可重新填入输入框）";
        // 
        // historyList
        // 
        historyList.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        historyList.BorderStyle = BorderStyle.FixedSingle;
        historyList.Font = new Font("Consolas", 10.5F);
        historyList.FormattingEnabled = true;
        historyList.IntegralHeight = false;
        historyList.ItemHeight = 17;
        historyList.Location = new Point(30, 112);
        historyList.Name = "historyList";
        historyList.Size = new Size(892, 316);
        historyList.TabIndex = 4;
        historyList.DoubleClick += historyList_DoubleClick;
        // 
        // statusLabel
        // 
        statusLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        statusLabel.AutoSize = true;
        statusLabel.Font = new Font("Microsoft YaHei UI", 9.5F);
        statusLabel.ForeColor = Color.FromArgb(100, 116, 139);
        statusLabel.Location = new Point(30, 448);
        statusLabel.Name = "statusLabel";
        statusLabel.Size = new Size(35, 19);
        statusLabel.TabIndex = 5;
        statusLabel.Text = "就绪";
        // 
        // fileLabel
        // 
        fileLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        fileLabel.AutoSize = true;
        fileLabel.Font = new Font("Consolas", 8.5F);
        fileLabel.ForeColor = Color.FromArgb(100, 116, 139);
        fileLabel.Location = new Point(30, 478);
        fileLabel.Name = "fileLabel";
        fileLabel.Size = new Size(42, 15);
        fileLabel.TabIndex = 6;
        fileLabel.Text = "JSON:";
        // 
        // hintLabel
        // 
        hintLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        hintLabel.Font = new Font("Microsoft YaHei UI", 9F);
        hintLabel.ForeColor = Color.FromArgb(100, 116, 139);
        hintLabel.Location = new Point(30, 504);
        hintLabel.Name = "hintLabel";
        hintLabel.Size = new Size(892, 38);
        hintLabel.TabIndex = 7;
        hintLabel.Text = "接入串口助手：写串口成功后调用 AddToHistory(text)；主窗体 FormClosing 时 SaveHistory()；启动时把 JSON 读回并刷新 ComboBox.Items。";
        // 
        // footerLabel
        // 
        footerLabel.Dock = DockStyle.Fill;
        footerLabel.Font = new Font("Microsoft YaHei UI", 9F);
        footerLabel.ForeColor = Color.FromArgb(100, 116, 139);
        footerLabel.Location = new Point(24, 672);
        footerLabel.Margin = new Padding(2, 4, 2, 0);
        footerLabel.Name = "footerLabel";
        footerLabel.Size = new Size(950, 32);
        footerLabel.TabIndex = 2;
        footerLabel.Text = "设计器提示：ComboBox、ListBox 和按钮都可在设计器里直接拖动调整，事件在“属性 → 事件”中查看。";
        footerLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // SendHistoryForm
        // 
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(244, 246, 250);
        ClientSize = new Size(1000, 720);
        Controls.Add(rootLayout);
        Font = new Font("Microsoft YaHei UI", 9.5F);
        MinimumSize = new Size(860, 640);
        Name = "SendHistoryForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "第 04 章练习 1 · 发送历史";
        FormClosing += SendHistoryForm_FormClosing;
        rootLayout.ResumeLayout(false);
        headerPanel.ResumeLayout(false);
        headerPanel.PerformLayout();
        contentPanel.ResumeLayout(false);
        contentPanel.PerformLayout();
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel rootLayout;
    private Panel headerPanel;
    private Label subtitleLabel;
    private Label titleLabel;
    private Panel contentPanel;
    private ComboBox historyCombo;
    private Button sendButton;
    private Button clearButton;
    private Label historyLabel;
    private ListBox historyList;
    private Label statusLabel;
    private Label fileLabel;
    private Label hintLabel;
    private Label footerLabel;
}
