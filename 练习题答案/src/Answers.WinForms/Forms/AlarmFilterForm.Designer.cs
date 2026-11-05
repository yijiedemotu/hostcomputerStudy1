namespace Answers.WinForms.Forms;

partial class AlarmFilterForm
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
        toolbarPanel = new FlowLayoutPanel();
        btnAll = new Button();
        btnAlarmOnly = new Button();
        btnFaultOnly = new Button();
        btnRecovered = new Button();
        autoCheck = new CheckBox();
        grid = new DataGridView();
        colTime = new DataGridViewTextBoxColumn();
        colDevice = new DataGridViewTextBoxColumn();
        colTag = new DataGridViewTextBoxColumn();
        colValue = new DataGridViewTextBoxColumn();
        colStatus = new DataGridViewTextBoxColumn();
        colMessage = new DataGridViewTextBoxColumn();
        footerLayout = new TableLayoutPanel();
        summaryLabel = new Label();
        hintLabel = new Label();
        footerLabel = new Label();
        autoTimer = new System.Windows.Forms.Timer(components);
        DataGridViewCellStyle headerStyle = new DataGridViewCellStyle();
        DataGridViewCellStyle rowStyle = new DataGridViewCellStyle();
        DataGridViewCellStyle alternateStyle = new DataGridViewCellStyle();
        rootLayout.SuspendLayout();
        headerPanel.SuspendLayout();
        contentPanel.SuspendLayout();
        contentLayout.SuspendLayout();
        toolbarPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)grid).BeginInit();
        footerLayout.SuspendLayout();
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
        rootLayout.Size = new Size(1080, 700);
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
        headerPanel.Size = new Size(1032, 86);
        headerPanel.TabIndex = 0;
        // 
        // badgeLabel
        // 
        badgeLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        badgeLabel.BackColor = Color.FromArgb(230, 242, 252);
        badgeLabel.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold);
        badgeLabel.ForeColor = Color.FromArgb(0, 95, 170);
        badgeLabel.Location = new Point(824, 8);
        badgeLabel.Name = "badgeLabel";
        badgeLabel.Size = new Size(206, 30);
        badgeLabel.TabIndex = 2;
        badgeLabel.Text = "DataGridView · BindingList";
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
        subtitleLabel.Text = "工具栏切换筛选条件；全量数据保留在 List 中，筛选只改变表格显示的子集。";
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
        titleLabel.Text = "第 04 章练习 2 · 报警筛选";
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
        contentPanel.Size = new Size(1032, 544);
        contentPanel.TabIndex = 1;
        // 
        // contentLayout
        // 
        contentLayout.ColumnCount = 1;
        contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        contentLayout.Controls.Add(toolbarPanel, 0, 0);
        contentLayout.Controls.Add(grid, 0, 1);
        contentLayout.Controls.Add(footerLayout, 0, 2);
        contentLayout.Dock = DockStyle.Fill;
        contentLayout.Location = new Point(20, 20);
        contentLayout.Margin = new Padding(0);
        contentLayout.Name = "contentLayout";
        contentLayout.RowCount = 3;
        contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
        contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
        contentLayout.Size = new Size(992, 504);
        contentLayout.TabIndex = 0;
        // 
        // toolbarPanel
        // 
        toolbarPanel.Controls.Add(btnAll);
        toolbarPanel.Controls.Add(btnAlarmOnly);
        toolbarPanel.Controls.Add(btnFaultOnly);
        toolbarPanel.Controls.Add(btnRecovered);
        toolbarPanel.Controls.Add(autoCheck);
        toolbarPanel.Dock = DockStyle.Fill;
        toolbarPanel.Location = new Point(0, 0);
        toolbarPanel.Margin = new Padding(0);
        toolbarPanel.Name = "toolbarPanel";
        toolbarPanel.Size = new Size(992, 50);
        toolbarPanel.TabIndex = 0;
        toolbarPanel.WrapContents = false;
        // 
        // btnAll
        // 
        btnAll.BackColor = Color.FromArgb(0, 120, 212);
        btnAll.Cursor = Cursors.Hand;
        btnAll.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        btnAll.FlatStyle = FlatStyle.Flat;
        btnAll.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold);
        btnAll.ForeColor = Color.White;
        btnAll.Location = new Point(0, 6);
        btnAll.Margin = new Padding(0, 6, 8, 0);
        btnAll.Name = "btnAll";
        btnAll.Size = new Size(96, 34);
        btnAll.TabIndex = 0;
        btnAll.Text = "全部";
        btnAll.UseVisualStyleBackColor = false;
        btnAll.Click += FilterButton_Click;
        // 
        // btnAlarmOnly
        // 
        btnAlarmOnly.BackColor = Color.White;
        btnAlarmOnly.Cursor = Cursors.Hand;
        btnAlarmOnly.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        btnAlarmOnly.FlatStyle = FlatStyle.Flat;
        btnAlarmOnly.Font = new Font("Microsoft YaHei UI", 9.5F);
        btnAlarmOnly.ForeColor = Color.FromArgb(31, 41, 55);
        btnAlarmOnly.Location = new Point(104, 6);
        btnAlarmOnly.Margin = new Padding(0, 6, 8, 0);
        btnAlarmOnly.Name = "btnAlarmOnly";
        btnAlarmOnly.Size = new Size(96, 34);
        btnAlarmOnly.TabIndex = 1;
        btnAlarmOnly.Text = "仅报警";
        btnAlarmOnly.UseVisualStyleBackColor = false;
        btnAlarmOnly.Click += FilterButton_Click;
        // 
        // btnFaultOnly
        // 
        btnFaultOnly.BackColor = Color.White;
        btnFaultOnly.Cursor = Cursors.Hand;
        btnFaultOnly.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        btnFaultOnly.FlatStyle = FlatStyle.Flat;
        btnFaultOnly.Font = new Font("Microsoft YaHei UI", 9.5F);
        btnFaultOnly.ForeColor = Color.FromArgb(31, 41, 55);
        btnFaultOnly.Location = new Point(208, 6);
        btnFaultOnly.Margin = new Padding(0, 6, 8, 0);
        btnFaultOnly.Name = "btnFaultOnly";
        btnFaultOnly.Size = new Size(96, 34);
        btnFaultOnly.TabIndex = 2;
        btnFaultOnly.Text = "仅故障";
        btnFaultOnly.UseVisualStyleBackColor = false;
        btnFaultOnly.Click += FilterButton_Click;
        // 
        // btnRecovered
        // 
        btnRecovered.BackColor = Color.White;
        btnRecovered.Cursor = Cursors.Hand;
        btnRecovered.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        btnRecovered.FlatStyle = FlatStyle.Flat;
        btnRecovered.Font = new Font("Microsoft YaHei UI", 9.5F);
        btnRecovered.ForeColor = Color.FromArgb(31, 41, 55);
        btnRecovered.Location = new Point(312, 6);
        btnRecovered.Margin = new Padding(0, 6, 8, 0);
        btnRecovered.Name = "btnRecovered";
        btnRecovered.Size = new Size(110, 34);
        btnRecovered.TabIndex = 3;
        btnRecovered.Text = "正常与恢复";
        btnRecovered.UseVisualStyleBackColor = false;
        btnRecovered.Click += FilterButton_Click;
        // 
        // autoCheck
        // 
        autoCheck.AutoSize = true;
        autoCheck.Font = new Font("Microsoft YaHei UI", 9.5F);
        autoCheck.ForeColor = Color.FromArgb(31, 41, 55);
        autoCheck.Location = new Point(446, 14);
        autoCheck.Margin = new Padding(16, 14, 0, 0);
        autoCheck.Name = "autoCheck";
        autoCheck.Size = new Size(192, 21);
        autoCheck.TabIndex = 4;
        autoCheck.Text = "自动模拟（每 1.5 秒一条）";
        autoCheck.UseVisualStyleBackColor = true;
        autoCheck.CheckedChanged += autoCheck_CheckedChanged;
        // 
        // grid
        // 
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        alternateStyle.BackColor = Color.FromArgb(248, 250, 252);
        grid.AlternatingRowsDefaultCellStyle = alternateStyle;
        grid.AutoGenerateColumns = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.BackgroundColor = Color.White;
        grid.BorderStyle = BorderStyle.None;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        headerStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        headerStyle.BackColor = Color.FromArgb(241, 245, 249);
        headerStyle.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        headerStyle.ForeColor = Color.FromArgb(31, 41, 55);
        headerStyle.SelectionBackColor = Color.FromArgb(241, 245, 249);
        headerStyle.WrapMode = DataGridViewTriState.False;
        grid.ColumnHeadersDefaultCellStyle = headerStyle;
        grid.ColumnHeadersHeight = 34;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.Columns.AddRange(new DataGridViewColumn[] { colTime, colDevice, colTag, colValue, colStatus, colMessage });
        rowStyle.BackColor = Color.White;
        rowStyle.ForeColor = Color.FromArgb(31, 41, 55);
        rowStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
        rowStyle.SelectionForeColor = Color.FromArgb(31, 41, 55);
        grid.DefaultCellStyle = rowStyle;
        grid.Dock = DockStyle.Fill;
        grid.EnableHeadersVisualStyles = false;
        grid.GridColor = Color.FromArgb(226, 232, 240);
        grid.Location = new Point(0, 50);
        grid.Margin = new Padding(0);
        grid.MultiSelect = false;
        grid.Name = "grid";
        grid.ReadOnly = true;
        grid.RowHeadersVisible = false;
        grid.RowTemplate.Height = 28;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.Size = new Size(992, 392);
        grid.TabIndex = 1;
        grid.CellFormatting += grid_CellFormatting;
        // 
        // colTime
        // 
        colTime.DataPropertyName = "Time";
        colTime.DefaultCellStyle.Format = "yyyy-MM-dd HH:mm:ss";
        colTime.FillWeight = 150F;
        colTime.HeaderText = "时间";
        colTime.MinimumWidth = 130;
        colTime.Name = "colTime";
        colTime.ReadOnly = true;
        colTime.SortMode = DataGridViewColumnSortMode.NotSortable;
        // 
        // colDevice
        // 
        colDevice.DataPropertyName = "Device";
        colDevice.FillWeight = 70F;
        colDevice.HeaderText = "设备";
        colDevice.MinimumWidth = 70;
        colDevice.Name = "colDevice";
        colDevice.ReadOnly = true;
        colDevice.SortMode = DataGridViewColumnSortMode.NotSortable;
        // 
        // colTag
        // 
        colTag.DataPropertyName = "Tag";
        colTag.FillWeight = 110F;
        colTag.HeaderText = "位号";
        colTag.MinimumWidth = 100;
        colTag.Name = "colTag";
        colTag.ReadOnly = true;
        colTag.SortMode = DataGridViewColumnSortMode.NotSortable;
        // 
        // colValue
        // 
        colValue.DataPropertyName = "Value";
        colValue.DefaultCellStyle.Format = "F2";
        colValue.FillWeight = 65F;
        colValue.HeaderText = "数值";
        colValue.MinimumWidth = 70;
        colValue.Name = "colValue";
        colValue.ReadOnly = true;
        colValue.SortMode = DataGridViewColumnSortMode.NotSortable;
        // 
        // colStatus
        // 
        colStatus.DataPropertyName = "Status";
        colStatus.FillWeight = 70F;
        colStatus.HeaderText = "状态";
        colStatus.MinimumWidth = 75;
        colStatus.Name = "colStatus";
        colStatus.ReadOnly = true;
        colStatus.SortMode = DataGridViewColumnSortMode.NotSortable;
        // 
        // colMessage
        // 
        colMessage.DataPropertyName = "Message";
        colMessage.FillWeight = 180F;
        colMessage.HeaderText = "说明";
        colMessage.MinimumWidth = 150;
        colMessage.Name = "colMessage";
        colMessage.ReadOnly = true;
        colMessage.SortMode = DataGridViewColumnSortMode.NotSortable;
        // 
        // footerLayout
        // 
        footerLayout.ColumnCount = 1;
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        footerLayout.Controls.Add(summaryLabel, 0, 0);
        footerLayout.Controls.Add(hintLabel, 0, 1);
        footerLayout.Dock = DockStyle.Fill;
        footerLayout.Location = new Point(0, 442);
        footerLayout.Margin = new Padding(0);
        footerLayout.Name = "footerLayout";
        footerLayout.RowCount = 2;
        footerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
        footerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        footerLayout.Size = new Size(992, 62);
        footerLayout.TabIndex = 2;
        // 
        // summaryLabel
        // 
        summaryLabel.Dock = DockStyle.Fill;
        summaryLabel.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold);
        summaryLabel.ForeColor = Color.FromArgb(31, 41, 55);
        summaryLabel.Location = new Point(0, 0);
        summaryLabel.Margin = new Padding(0);
        summaryLabel.Name = "summaryLabel";
        summaryLabel.Size = new Size(992, 26);
        summaryLabel.TabIndex = 0;
        summaryLabel.Text = "当前筛选：全部";
        summaryLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // hintLabel
        // 
        hintLabel.Dock = DockStyle.Fill;
        hintLabel.Font = new Font("Microsoft YaHei UI", 9F);
        hintLabel.ForeColor = Color.FromArgb(100, 116, 139);
        hintLabel.Location = new Point(0, 26);
        hintLabel.Margin = new Padding(0);
        hintLabel.Name = "hintLabel";
        hintLabel.Size = new Size(992, 36);
        hintLabel.TabIndex = 1;
        hintLabel.Text = "筛选只改变表格显示的数据，不修改原始 List。表格用 BindingList 重新绑定，状态列由 CellFormatting 上色。";
        hintLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // footerLabel
        // 
        footerLabel.Dock = DockStyle.Fill;
        footerLabel.Font = new Font("Microsoft YaHei UI", 9F);
        footerLabel.ForeColor = Color.FromArgb(100, 116, 139);
        footerLabel.Location = new Point(24, 652);
        footerLabel.Margin = new Padding(2, 4, 2, 0);
        footerLabel.Name = "footerLabel";
        footerLabel.Size = new Size(1030, 32);
        footerLabel.TabIndex = 2;
        footerLabel.Text = "设计器提示：表格的列、DataPropertyName、列宽都在 Designer.cs 里，可直接在设计器中增删列、修改标题。";
        footerLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // autoTimer
        // 
        autoTimer.Interval = 1500;
        autoTimer.Tick += autoTimer_Tick;
        // 
        // AlarmFilterForm
        // 
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(244, 246, 250);
        ClientSize = new Size(1080, 700);
        Controls.Add(rootLayout);
        Font = new Font("Microsoft YaHei UI", 9.5F);
        MinimumSize = new Size(920, 620);
        Name = "AlarmFilterForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "第 04 章练习 2 · 报警筛选";
        rootLayout.ResumeLayout(false);
        headerPanel.ResumeLayout(false);
        headerPanel.PerformLayout();
        contentPanel.ResumeLayout(false);
        contentLayout.ResumeLayout(false);
        toolbarPanel.ResumeLayout(false);
        toolbarPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)grid).EndInit();
        footerLayout.ResumeLayout(false);
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
    private FlowLayoutPanel toolbarPanel;
    private Button btnAll;
    private Button btnAlarmOnly;
    private Button btnFaultOnly;
    private Button btnRecovered;
    private CheckBox autoCheck;
    private DataGridView grid;
    private DataGridViewTextBoxColumn colTime;
    private DataGridViewTextBoxColumn colDevice;
    private DataGridViewTextBoxColumn colTag;
    private DataGridViewTextBoxColumn colValue;
    private DataGridViewTextBoxColumn colStatus;
    private DataGridViewTextBoxColumn colMessage;
    private TableLayoutPanel footerLayout;
    private Label summaryLabel;
    private Label hintLabel;
    private Label footerLabel;
    private System.Windows.Forms.Timer autoTimer;
}
