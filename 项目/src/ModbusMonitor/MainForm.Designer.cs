namespace ModbusMonitor;

/// <summary>
/// 主窗口布局代码。界面结构：
/// 顶部工具栏（启动/停止/重载/数据目录） + 中间三个页签（实时/报警/日志） + 底部状态栏。
/// 业务逻辑都在 MainForm.cs，这里只负责“长什么样”。
/// </summary>
partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null)) components.Dispose();
        base.Dispose(disposing);
    }

    /// <summary>创建所有控件、设置属性、绑定事件。</summary>
    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        toolStrip = new ToolStrip();
        btnStart = new ToolStripButton();
        btnStop = new ToolStripButton();
        btnReload = new ToolStripButton();
        sep1 = new ToolStripSeparator();
        btnOpenFolder = new ToolStripButton();
        lblMode = new ToolStripLabel();

        statusStrip = new StatusStrip();
        lblDeviceState = new ToolStripStatusLabel();
        lblRecords = new ToolStripStatusLabel();
        lblTips = new ToolStripStatusLabel();

        tabMain = new TabControl();
        tabRealtime = new TabPage();
        tabAlarms = new TabPage();
        tabLog = new TabPage();

        splitRealtime = new SplitContainer();
        gridTags = new DataGridView();
        trendChart = new TrendChart();
        gridAlarms = new DataGridView();
        listLog = new ListBox();
        menuTags = new ContextMenuStrip(components);
        miWrite = new ToolStripMenuItem();
        miCopy = new ToolStripMenuItem();

        tmrFlush = new System.Windows.Forms.Timer(components);

        // 暂时挂起布局计算，全部设置完再统一恢复，避免界面闪烁
        toolStrip.SuspendLayout();
        statusStrip.SuspendLayout();
        tabMain.SuspendLayout();
        tabRealtime.SuspendLayout();
        tabAlarms.SuspendLayout();
        tabLog.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)splitRealtime).BeginInit();
        splitRealtime.Panel1.SuspendLayout();
        splitRealtime.Panel2.SuspendLayout();
        splitRealtime.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)gridTags).BeginInit();
        ((System.ComponentModel.ISupportInitialize)gridAlarms).BeginInit();
        SuspendLayout();

        // ---------- 工具栏 ----------
        toolStrip.GripStyle = ToolStripGripStyle.Hidden;
        toolStrip.ImageScalingSize = new Size(20, 20);
        toolStrip.Padding = new Padding(8, 2, 8, 2);
        toolStrip.BackColor = Color.FromArgb(32, 36, 44);
        toolStrip.ForeColor = Color.Gainsboro;
        toolStrip.RenderMode = ToolStripRenderMode.System;

        btnStart.Text = "▶ 启动采集";
        btnStart.ForeColor = Color.Gainsboro;
        btnStart.Click += btnStart_Click;

        btnStop.Text = "■ 停止";
        btnStop.ForeColor = Color.Gainsboro;
        btnStop.Enabled = false;
        btnStop.Click += btnStop_Click;

        btnReload.Text = "⟳ 重载配置";
        btnReload.ForeColor = Color.Gainsboro;
        btnReload.Click += btnReload_Click;

        btnOpenFolder.Text = "📁 数据目录";
        btnOpenFolder.ForeColor = Color.Gainsboro;
        btnOpenFolder.Click += btnOpenFolder_Click;

        lblMode.Text = "未启动";
        lblMode.ForeColor = Color.Gray;
        lblMode.Margin = new Padding(16, 0, 0, 0);

        toolStrip.Items.AddRange(new ToolStripItem[]
        {
            btnStart, btnStop, btnReload, sep1, btnOpenFolder, lblMode
        });

        // ---------- 状态栏 ----------
        lblDeviceState.Text = "设备：--";
        lblDeviceState.BorderSides = ToolStripStatusLabelBorderSides.Right;
        lblRecords.Text = "历史 0 条";
        lblRecords.BorderSides = ToolStripStatusLabelBorderSides.Right;
        lblTips.Spring = true;
        lblTips.TextAlign = ContentAlignment.MiddleRight;
        lblTips.Text = "双击数据目录可查看 CSV 历史；右键表格可写入寄存器";

        statusStrip.Items.AddRange(new ToolStripItem[] { lblDeviceState, lblRecords, lblTips });
        statusStrip.BackColor = Color.FromArgb(32, 36, 44);
        statusStrip.ForeColor = Color.Gainsboro;

        // ---------- 实时数据页 ----------
        splitRealtime.Dock = DockStyle.Fill;
        splitRealtime.Orientation = Orientation.Vertical;

        // 必须先给一个足够大的初始尺寸，再设置两个 Panel 的最小尺寸。
        // SplitContainer 构造时默认只有 150 像素宽，如果先设 Panel2MinSize = 300，
        // EndInit 时会抛“SplitterDistance 必须介于 Panel1MinSize 与 Width - Panel2MinSize 之间”。
        // Dock = Fill 之后，这里的尺寸只是初始值，运行时会由布局重新计算。
        splitRealtime.Size = new Size(1200, 600);
        splitRealtime.SplitterWidth = 6;
        splitRealtime.Panel1MinSize = 320;
        splitRealtime.Panel2MinSize = 300;

        // 实时数据表格：只读、整行选中、列宽自动铺满。
        // AutoGenerateColumns = true 会读取 TagRow 上的 [DisplayName] 自动生成列。
        gridTags.Dock = DockStyle.Fill;
        gridTags.AutoGenerateColumns = true;
        gridTags.AllowUserToAddRows = false;
        gridTags.AllowUserToDeleteRows = false;
        gridTags.AllowUserToResizeRows = false;
        gridTags.ReadOnly = true;
        gridTags.RowHeadersVisible = false;
        gridTags.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        gridTags.MultiSelect = false;
        gridTags.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        gridTags.BackgroundColor = Color.FromArgb(26, 28, 34);
        gridTags.GridColor = Color.FromArgb(52, 56, 64);
        gridTags.EnableHeadersVisualStyles = false;
        gridTags.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(40, 44, 52);
        gridTags.ColumnHeadersDefaultCellStyle.ForeColor = Color.Gainsboro;
        gridTags.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        gridTags.ColumnHeadersHeight = 34;
        gridTags.DefaultCellStyle.BackColor = Color.FromArgb(26, 28, 34);
        gridTags.DefaultCellStyle.ForeColor = Color.Gainsboro;
        gridTags.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 90, 120);
        gridTags.DefaultCellStyle.SelectionForeColor = Color.White;
        gridTags.DefaultCellStyle.Font = new Font("Microsoft YaHei UI", 10F);
        gridTags.RowTemplate.Height = 30;
        gridTags.ContextMenuStrip = menuTags;
        gridTags.SelectionChanged += gridTags_SelectionChanged;
        gridTags.CellFormatting += gridTags_CellFormatting;

        trendChart.Dock = DockStyle.Fill;
        trendChart.Margin = new Padding(0);

        splitRealtime.Panel1.Controls.Add(gridTags);
        splitRealtime.Panel2.Controls.Add(trendChart);
        splitRealtime.SplitterDistance = 560;

        tabRealtime.Text = "实时数据";
        tabRealtime.UseVisualStyleBackColor = true;
        tabRealtime.Controls.Add(splitRealtime);

        // ---------- 报警页 ----------
        gridAlarms.Dock = DockStyle.Fill;
        gridAlarms.AutoGenerateColumns = true;
        gridAlarms.AllowUserToAddRows = false;
        gridAlarms.ReadOnly = true;
        gridAlarms.RowHeadersVisible = false;
        gridAlarms.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        gridAlarms.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        gridAlarms.BackgroundColor = Color.FromArgb(26, 28, 34);
        gridAlarms.GridColor = Color.FromArgb(52, 56, 64);
        gridAlarms.EnableHeadersVisualStyles = false;
        gridAlarms.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(40, 44, 52);
        gridAlarms.ColumnHeadersDefaultCellStyle.ForeColor = Color.Gainsboro;
        gridAlarms.DefaultCellStyle.BackColor = Color.FromArgb(26, 28, 34);
        gridAlarms.DefaultCellStyle.ForeColor = Color.Gainsboro;
        gridAlarms.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 90, 120);
        gridAlarms.RowTemplate.Height = 28;

        tabAlarms.Text = "报警记录";
        tabAlarms.Controls.Add(gridAlarms);

        // ---------- 日志页 ----------
        listLog.Dock = DockStyle.Fill;
        listLog.BackColor = Color.FromArgb(18, 20, 24);
        listLog.ForeColor = Color.FromArgb(150, 220, 180);
        listLog.Font = new Font("Consolas", 9F);
        listLog.HorizontalScrollbar = true;
        listLog.IntegralHeight = false;
        tabLog.Text = "运行日志";
        tabLog.Controls.Add(listLog);

        tabMain.Dock = DockStyle.Fill;
        tabMain.Padding = new Point(12, 6);
        tabMain.Controls.Add(tabRealtime);
        tabMain.Controls.Add(tabAlarms);
        tabMain.Controls.Add(tabLog);

        // ---------- 右键菜单 ----------
        miWrite.Text = "写入该点位（06/10 功能码）…";
        miWrite.Click += miWrite_Click;
        miCopy.Text = "复制数值";
        miCopy.Click += miCopy_Click;
        menuTags.Items.AddRange(new ToolStripItem[] { miWrite, miCopy });

        tmrFlush.Interval = 1000;
        tmrFlush.Tick += tmrFlush_Tick;

        // ---------- 窗体 ----------
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1280, 760);
        MinimumSize = new Size(1000, 620);
        Text = "工业数据采集上位机 · Modbus 监控 Demo";
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(22, 24, 28);
        FormClosing += MainForm_FormClosing;
        Load += MainForm_Load;

        // 后 Add 的先占位：tabMain 填满剩余空间，状态栏贴底，工具栏贴顶
        Controls.Add(tabMain);
        Controls.Add(statusStrip);
        Controls.Add(toolStrip);

        toolStrip.ResumeLayout(false);
        toolStrip.PerformLayout();
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        tabMain.ResumeLayout(false);
        tabRealtime.ResumeLayout(false);
        tabAlarms.ResumeLayout(false);
        tabLog.ResumeLayout(false);
        splitRealtime.Panel1.ResumeLayout(false);
        splitRealtime.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)splitRealtime).EndInit();
        splitRealtime.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)gridTags).EndInit();
        ((System.ComponentModel.ISupportInitialize)gridAlarms).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    // ---- 工具栏 ----
    private ToolStrip toolStrip;
    private ToolStripButton btnStart;
    private ToolStripButton btnStop;
    private ToolStripButton btnReload;
    private ToolStripSeparator sep1;
    private ToolStripButton btnOpenFolder;
    private ToolStripLabel lblMode;

    // ---- 状态栏 ----
    private StatusStrip statusStrip;
    private ToolStripStatusLabel lblDeviceState;
    private ToolStripStatusLabel lblRecords;
    private ToolStripStatusLabel lblTips;

    // ---- 页签与内容 ----
    private TabControl tabMain;
    private TabPage tabRealtime;
    private TabPage tabAlarms;
    private TabPage tabLog;

    private SplitContainer splitRealtime;
    private DataGridView gridTags;
    private TrendChart trendChart;
    private DataGridView gridAlarms;
    private ListBox listLog;

    private ContextMenuStrip menuTags;
    private ToolStripMenuItem miWrite;
    private ToolStripMenuItem miCopy;

    private System.Windows.Forms.Timer tmrFlush;
}
