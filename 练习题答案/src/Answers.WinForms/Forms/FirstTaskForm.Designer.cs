namespace Answers.WinForms.Forms;

partial class FirstTaskForm
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
        inputBox = new TextBox();
        redCheck = new CheckBox();
        convertButton = new Button();
        step1Label = new Label();
        step2Label = new Label();
        resultLabel = new Label();
        noteLabel = new Label();
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
        rootLayout.Size = new Size(960, 640);
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
        headerPanel.Size = new Size(912, 86);
        headerPanel.TabIndex = 0;
        // 
        // subtitleLabel
        // 
        subtitleLabel.AutoSize = true;
        subtitleLabel.Font = new Font("Microsoft YaHei UI", 9.5F);
        subtitleLabel.ForeColor = Color.FromArgb(100, 116, 139);
        subtitleLabel.Location = new Point(2, 52);
        subtitleLabel.Name = "subtitleLabel";
        subtitleLabel.Size = new Size(552, 19);
        subtitleLabel.TabIndex = 1;
        subtitleLabel.Text = "控件 + 事件 + 字符串 + 条件判断 + 属性修改，是后面所有复杂界面的最小组合。";
        // 
        // titleLabel
        // 
        titleLabel.AutoSize = true;
        titleLabel.Font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold);
        titleLabel.ForeColor = Color.FromArgb(31, 41, 55);
        titleLabel.Location = new Point(0, 8);
        titleLabel.Name = "titleLabel";
        titleLabel.Size = new Size(261, 31);
        titleLabel.TabIndex = 0;
        titleLabel.Text = "第 01 章 · 第一个小任务";
        // 
        // contentPanel
        // 
        contentPanel.BackColor = Color.White;
        contentPanel.Controls.Add(inputBox);
        contentPanel.Controls.Add(redCheck);
        contentPanel.Controls.Add(convertButton);
        contentPanel.Controls.Add(step1Label);
        contentPanel.Controls.Add(step2Label);
        contentPanel.Controls.Add(resultLabel);
        contentPanel.Controls.Add(noteLabel);
        contentPanel.Dock = DockStyle.Fill;
        contentPanel.Location = new Point(24, 104);
        contentPanel.Margin = new Padding(0);
        contentPanel.Name = "contentPanel";
        contentPanel.Padding = new Padding(28);
        contentPanel.Size = new Size(912, 484);
        contentPanel.TabIndex = 1;
        // 
        // inputBox
        // 
        inputBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        inputBox.BorderStyle = BorderStyle.FixedSingle;
        inputBox.Font = new Font("Microsoft YaHei UI", 12F);
        inputBox.Location = new Point(30, 56);
        inputBox.Name = "inputBox";
        inputBox.PlaceholderText = "例如：hello modbus";
        inputBox.Size = new Size(840, 28);
        inputBox.TabIndex = 0;
        inputBox.KeyDown += inputBox_KeyDown;
        // 
        // redCheck
        // 
        redCheck.AutoSize = true;
        redCheck.Font = new Font("Microsoft YaHei UI", 9.5F);
        redCheck.ForeColor = Color.FromArgb(31, 41, 55);
        redCheck.Location = new Point(32, 108);
        redCheck.Name = "redCheck";
        redCheck.Size = new Size(196, 21);
        redCheck.TabIndex = 1;
        redCheck.Text = "勾选后，结果显示为红色";
        redCheck.UseVisualStyleBackColor = true;
        redCheck.CheckedChanged += redCheck_CheckedChanged;
        // 
        // convertButton
        // 
        convertButton.BackColor = Color.FromArgb(0, 120, 212);
        convertButton.Cursor = Cursors.Hand;
        convertButton.FlatAppearance.BorderSize = 0;
        convertButton.FlatStyle = FlatStyle.Flat;
        convertButton.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold);
        convertButton.ForeColor = Color.White;
        convertButton.Location = new Point(30, 146);
        convertButton.Name = "convertButton";
        convertButton.Size = new Size(128, 38);
        convertButton.TabIndex = 2;
        convertButton.Text = "转成大写";
        convertButton.UseVisualStyleBackColor = false;
        convertButton.Click += convertButton_Click;
        // 
        // step1Label
        // 
        step1Label.AutoSize = true;
        step1Label.Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold);
        step1Label.ForeColor = Color.FromArgb(31, 41, 55);
        step1Label.Location = new Point(28, 20);
        step1Label.Name = "step1Label";
        step1Label.Size = new Size(98, 19);
        step1Label.TabIndex = 3;
        step1Label.Text = "1. 输入内容";
        // 
        // step2Label
        // 
        step2Label.AutoSize = true;
        step2Label.Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold);
        step2Label.ForeColor = Color.FromArgb(31, 41, 55);
        step2Label.Location = new Point(28, 212);
        step2Label.Name = "step2Label";
        step2Label.Size = new Size(70, 19);
        step2Label.TabIndex = 4;
        step2Label.Text = "2. 结果";
        // 
        // resultLabel
        // 
        resultLabel.AutoSize = true;
        resultLabel.Font = new Font("Microsoft YaHei UI", 22F, FontStyle.Bold);
        resultLabel.ForeColor = Color.FromArgb(100, 116, 139);
        resultLabel.Location = new Point(28, 248);
        resultLabel.Name = "resultLabel";
        resultLabel.Size = new Size(230, 40);
        resultLabel.TabIndex = 5;
        resultLabel.Text = "结果会显示在这里";
        // 
        // noteLabel
        // 
        noteLabel.AutoSize = true;
        noteLabel.Font = new Font("Microsoft YaHei UI", 9.5F);
        noteLabel.ForeColor = Color.FromArgb(100, 116, 139);
        noteLabel.Location = new Point(30, 330);
        noteLabel.MaximumSize = new Size(840, 0);
        noteLabel.Name = "noteLabel";
        noteLabel.Size = new Size(802, 57);
        noteLabel.TabIndex = 6;
        noteLabel.Text = "验收点：输入为空弹窗“请输入内容”；大写转换正确；颜色随 CheckBox 变化；按回车也能触发。\r\n\r\n代码分工：FirstTaskForm.Designer.cs 负责界面，FirstTaskForm.cs 负责事件逻辑。";
        // 
        // footerLabel
        // 
        footerLabel.Dock = DockStyle.Fill;
        footerLabel.Font = new Font("Microsoft YaHei UI", 9F);
        footerLabel.ForeColor = Color.FromArgb(100, 116, 139);
        footerLabel.Location = new Point(24, 592);
        footerLabel.Margin = new Padding(2, 4, 2, 0);
        footerLabel.Name = "footerLabel";
        footerLabel.Size = new Size(910, 32);
        footerLabel.TabIndex = 2;
        footerLabel.Text = "设计器提示：选中控件后可在“属性”窗口直接改 Text / Font / ForeColor，保存时设计器会更新 .Designer.cs。";
        footerLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // FirstTaskForm
        // 
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(244, 246, 250);
        ClientSize = new Size(960, 640);
        Controls.Add(rootLayout);
        Font = new Font("Microsoft YaHei UI", 9.5F);
        MinimumSize = new Size(820, 600);
        Name = "FirstTaskForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "第 01 章 · 第一个小任务";
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
    private TextBox inputBox;
    private CheckBox redCheck;
    private Button convertButton;
    private Label step1Label;
    private Label step2Label;
    private Label resultLabel;
    private Label noteLabel;
    private Label footerLabel;
}
