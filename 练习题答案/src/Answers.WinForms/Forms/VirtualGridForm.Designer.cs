namespace Answers.WinForms.Forms;

partial class VirtualGridForm
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
        btnReload = new Button();
        btnJump = new Button();
        btnBottom = new Button();
        btnTop = new Button();
        grid = new DataGridView();
        colIndex = new DataGridViewTextBoxColumn();
        colTime = new DataGridViewTextBoxColumn();
        colPoint = new DataGridViewTextBoxColumn();
        colValue = new DataGridViewTextBoxColumn();
        colStatus = new DataGridViewTextBoxColumn();
        colNote = new DataGridViewTextBoxColumn();
        footerLayout = new TableLayoutPanel();
        generationLabel = new Label();
        visibleLabel = new Label();
        requestLabel = new Label();
        footerLabel = new Label();
        statsTimer = new System.Windows.Forms.Timer(components);
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
        rootLayout.Size = new Size(1100, 720);
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
        headerPanel.Size = new Size(1052, 86);
        headerPanel.TabIndex = 0;
        // 
        // badgeLabel
        // 
        badgeLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        badgeLabel.BackColor = Color.FromArgb(230, 242, 252);
        badgeLabel.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold);
        badgeLabel.ForeColor = Color.FromArgb(0, 95, 170);
        badgeLabel.Location = new Point(832, 8);
        badgeLabel.Name = "badgeLabel";
        badgeLabel.Size = new Size(220, 30);
        badgeLabel.TabIndex = 2;
        badgeLabel.Text = "VirtualMode · 100,000 Rows";
        badgeLabel.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // subtitleLabel
        // 
        subtitleLabel.AutoSize = true;
        subtitleLabel.Font = new Font("Microsoft YaHei UI", 9.5F);
        subtitleLabel.ForeColor = Color.FromArgb(100, 116, 139);
        subtitleLabel.Location = new Point(2, 52);
        subtitleLabel.Name = "subtitleLabel";
        subtitleLabel.Size = new Size(680, 19);
        subtitleLabel.TabIndex = 1;
        subtitleLabel.Text = "数据放在结构体数组，表格只向你要“当前可见行”的单元格，滚动时留意底部计数。";
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
        titleLabel.Text = "第 04 章练习 4 · 虚拟表格";
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
        contentPanel.Size = new Size(1052, 564);
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
        contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 88F));
        contentLayout.Size = new Size(1012, 524);
        contentLayout.TabIndex = 0;
        // 
        // toolbarPanel
        // 
        toolbarPanel.Controls.Add(btnReload);
        toolbarPanel.Controls.Add(btnJump);
        toolbarPanel.Controls.Add(btnBottom);
        toolbarPanel.Controls.Add(btnTop);
        toolbarPanel.Dock = DockStyle.Fill;
        toolbarPanel.Location = new Point(0, 0);
        toolbarPanel.Margin = new Padding(0);
        toolbarPanel.Name = "toolbarPanel";
        toolbarPanel.Size = new Size(1012, 50);
        toolbarPanel.TabIndex = 0;
        toolbarPanel.WrapContents = false;
        // 
        // btnReload
        // 
        btnReload.BackColor = Color.FromArgb(0, 120, 212);
        btnReload.Cursor = Cursors.Hand;
        btnReload.FlatAppearance.BorderSize = 0;
        btnReload.FlatStyle = FlatStyle.Flat;
        btnReload.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold);
        btnReload.ForeColor = Color.White;
        btnReload.Location = new Point(0, 6);
        btnReload.Margin = new Padding(0, 6, 8, 0);
        btnReload.Name = "btnReload";
        btnReload.Size = new Size(150, 34);
        btnReload.TabIndex = 0;
        btnReload.Text = "重新生成 10 万行";
        btnReload.UseVisualStyleBackColor = false;
        btnReload.Click += btnReload_Click;
        // 
        // btnJump
        // 
        btnJump.BackColor = Color.White;
        btnJump.Cursor = Cursors.Hand;
        btnJump.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        btnJump.FlatStyle = FlatStyle.Flat;
        btnJump.Font = new Font("Microsoft YaHei UI", 9.5F);
        btnJump.ForeColor = Color.FromArgb(31, 41, 55);
        btnJump.Location = new Point(158, 6);
        btnJump.Margin = new Padding(0, 6, 8, 0);
        btnJump.Name = "btnJump";
        btnJump.Size = new Size(150, 34);
        btnJump.TabIndex = 1;
        btnJump.Text = "跳到第 50,000 行";
        btnJump.UseVisualStyleBackColor = false;
        btnJump.Click += btnJump_Click;
        // 
        // btnBottom
        // 
        btnBottom.BackColor = Color.White;
        btnBottom.Cursor = Cursors.Hand;
        btnBottom.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        btnBottom.FlatStyle = FlatStyle.Flat;
        btnBottom.Font = new Font("Microsoft YaHei UI", 9.5F);
        btnBottom.ForeColor = Color.FromArgb(31, 41, 55);
        btnBottom.Location = new Point(316, 6);
        btnBottom.Margin = new Padding(0, 6, 8, 0);
        btnBottom.Name = "btnBottom";
        btnBottom.Size = new Size(118, 34);
        btnBottom.TabIndex = 2;
        btnBottom.Text = "滚动到底部";
        btnBottom.UseVisualStyleBackColor = false;
        btnBottom.Click += btnBottom_Click;
        // 
        // btnTop
        // 
        btnTop.BackColor = Color.White;
        btnTop.Cursor = Cursors.Hand;
        btnTop.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        btnTop.FlatStyle = FlatStyle.Flat;
        btnTop.Font = new Font("Microsoft YaHei UI", 9.5F);
        btnTop.ForeColor = Color.FromArgb(31, 41, 55);
        btnTop.Location = new Point(442, 6);
        btnTop.Margin = new Padding(0, 6, 8, 0);
        btnTop.Name = "btnTop";
        btnTop.Size = new Size(104, 34);
        btnTop.TabIndex = 3;
        btnTop.Text = "回到顶部";
        btnTop.UseVisualStyleBackColor = false;
        btnTop.Click += btnTop_Click;
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
        grid.Columns.AddRange(new DataGridViewColumn[] { colIndex, colTime, colPoint, colValue, colStatus, colNote });
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
        grid.Size = new Size(1012, 386);
        grid.TabIndex = 1;
        grid.VirtualMode = true;
        grid.CellFormatting += grid_CellFormatting;
        grid.CellValueNeeded += grid_CellValueNeeded;
        grid.Scroll += grid_Scroll;
        // 
        // colIndex
        // 
        colIndex.FillWeight = 55F;
        colIndex.HeaderText = "序号";
        colIndex.MinimumWidth = 60;
        colIndex.Name = "colIndex";
        colIndex.ReadOnly = true;
        colIndex.SortMode = DataGridViewColumnSortMode.NotSortable;
        // 
        // colTime
        // 
        colTime.FillWeight = 145F;
        colTime.HeaderText = "采集时间";
        colTime.MinimumWidth = 150;
        colTime.Name = "colTime";
        colTime.ReadOnly = true;
        colTime.SortMode = DataGridViewColumnSortMode.NotSortable;
        // 
        // colPoint
        // 
        colPoint.FillWeight = 110F;
        colPoint.HeaderText = "点位名称";
        colPoint.MinimumWidth = 110;
        colPoint.Name = "colPoint";
        colPoint.ReadOnly = true;
        colPoint.SortMode = DataGridViewColumnSortMode.NotSortable;
        // 
        // colValue
        // 
        colValue.FillWeight = 75F;
        colValue.HeaderText = "数值";
        colValue.MinimumWidth = 80;
        colValue.Name = "colValue";
        colValue.ReadOnly = true;
        colValue.SortMode = DataGridViewColumnSortMode.NotSortable;
        // 
        // colStatus
        // 
        colStatus.FillWeight = 70F;
        colStatus.HeaderText = "状态";
        colStatus.MinimumWidth = 75;
        colStatus.Name = "colStatus";
        colStatus.ReadOnly = true;
        colStatus.SortMode = DataGridViewColumnSortMode.NotSortable;
        // 
        // colNote
        // 
        colNote.FillWeight = 180F;
        colNote.HeaderText = "备注";
        colNote.MinimumWidth = 150;
        colNote.Name = "colNote";
        colNote.ReadOnly = true;
        colNote.SortMode = DataGridViewColumnSortMode.NotSortable;
        // 
        // footerLayout
        // 
        footerLayout.ColumnCount = 1;
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        footerLayout.Controls.Add(generationLabel, 0, 0);
        footerLayout.Controls.Add(visibleLabel, 0, 1);
        footerLayout.Controls.Add(requestLabel, 0, 2);
        footerLayout.Dock = DockStyle.Fill;
        footerLayout.Location = new Point(0, 436);
        footerLayout.Margin = new Padding(0);
        footerLayout.Name = "footerLayout";
        footerLayout.RowCount = 3;
        footerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        footerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        footerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        footerLayout.Size = new Size(1012, 88);
        footerLayout.TabIndex = 2;
        // 
        // generationLabel
        // 
        generationLabel.Dock = DockStyle.Fill;
        generationLabel.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold);
        generationLabel.ForeColor = Color.FromArgb(31, 41, 55);
        generationLabel.Location = new Point(0, 0);
        generationLabel.Margin = new Padding(0);
        generationLabel.Name = "generationLabel";
        generationLabel.Size = new Size(1012, 28);
        generationLabel.TabIndex = 0;
        generationLabel.Text = "正在生成 10 万行…";
        generationLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // visibleLabel
        // 
        visibleLabel.Dock = DockStyle.Fill;
        visibleLabel.Font = new Font("Microsoft YaHei UI", 9.5F);
        visibleLabel.ForeColor = Color.FromArgb(31, 41, 55);
        visibleLabel.Location = new Point(0, 28);
        visibleLabel.Margin = new Padding(0);
        visibleLabel.Name = "visibleLabel";
        visibleLabel.Size = new Size(1012, 28);
        visibleLabel.TabIndex = 1;
        visibleLabel.Text = "当前可见行范围";
        visibleLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // requestLabel
        // 
        requestLabel.Dock = DockStyle.Fill;
        requestLabel.Font = new Font("Microsoft YaHei UI", 9F);
        requestLabel.ForeColor = Color.FromArgb(100, 116, 139);
        requestLabel.Location = new Point(0, 56);
        requestLabel.Margin = new Padding(0);
        requestLabel.Name = "requestLabel";
        requestLabel.Size = new Size(1012, 32);
        requestLabel.TabIndex = 2;
        requestLabel.Text = "CellValueNeeded：等待统计…";
        requestLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // footerLabel
        // 
        footerLabel.Dock = DockStyle.Fill;
        footerLabel.Font = new Font("Microsoft YaHei UI", 9F);
        footerLabel.ForeColor = Color.FromArgb(100, 116, 139);
        footerLabel.Location = new Point(24, 672);
        footerLabel.Margin = new Padding(2, 4, 2, 0);
        footerLabel.Name = "footerLabel";
        footerLabel.Size = new Size(1050, 32);
        footerLabel.TabIndex = 2;
        footerLabel.Text = "设计器提示：表格已勾选 VirtualMode；列只要定义标题，行数由代码里的 RowCount 提供。";
        footerLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // statsTimer
        // 
        statsTimer.Interval = 1000;
        statsTimer.Tick += statsTimer_Tick;
        // 
        // VirtualGridForm
        // 
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(244, 246, 250);
        ClientSize = new Size(1100, 720);
        Controls.Add(rootLayout);
        Font = new Font("Microsoft YaHei UI", 9.5F);
        MinimumSize = new Size(940, 620);
        Name = "VirtualGridForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "第 04 章练习 4 · 虚拟表格";
        rootLayout.ResumeLayout(false);
        headerPanel.ResumeLayout(false);
        headerPanel.PerformLayout();
        contentPanel.ResumeLayout(false);
        contentLayout.ResumeLayout(false);
        toolbarPanel.ResumeLayout(false);
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
    private Button btnReload;
    private Button btnJump;
    private Button btnBottom;
    private Button btnTop;
    private DataGridView grid;
    private DataGridViewTextBoxColumn colIndex;
    private DataGridViewTextBoxColumn colTime;
    private DataGridViewTextBoxColumn colPoint;
    private DataGridViewTextBoxColumn colValue;
    private DataGridViewTextBoxColumn colStatus;
    private DataGridViewTextBoxColumn colNote;
    private TableLayoutPanel footerLayout;
    private Label generationLabel;
    private Label visibleLabel;
    private Label requestLabel;
    private Label footerLabel;
    private System.Windows.Forms.Timer statsTimer;
}
