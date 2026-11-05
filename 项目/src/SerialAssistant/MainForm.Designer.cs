namespace SerialAssistant;

/// <summary>
/// 界面布局代码（通常由 Visual Studio 设计器自动生成，这里手写并加注释）。
/// 所有控件都在 InitializeComponent 里创建、设置属性、绑定事件。
/// 业务逻辑不写在这里，放在 MainForm.cs。
/// </summary>
partial class MainForm
{
    // 存放非可视组件（这里是两个 Timer），窗口关闭时统一释放
    private System.ComponentModel.IContainer components = null;

    /// <summary>窗口关闭时释放资源。designer 自动生成，一般不用改。</summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null)) components.Dispose();
        base.Dispose(disposing);
    }

    /// <summary>
    /// 创建并布置所有控件。
    /// 阅读顺序建议：先看每个区域的 Dock 属性，再看 Controls.Add 的顺序，
    /// 就能明白控件是怎么一层层叠上去的。
    /// </summary>
    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        panelParams = new FlowLayoutPanel();
        lblPort = new Label();
        cboPort = new ComboBox();
        btnRefreshPorts = new Button();
        lblBaud = new Label();
        cboBaud = new ComboBox();
        lblDataBits = new Label();
        cboDataBits = new ComboBox();
        lblParity = new Label();
        cboParity = new ComboBox();
        lblStopBits = new Label();
        cboStopBits = new ComboBox();
        btnOpen = new Button();

        panelOptions = new FlowLayoutPanel();
        chkHexDisplay = new CheckBox();
        chkTimestamp = new CheckBox();
        chkAutoScroll = new CheckBox();
        chkPause = new CheckBox();
        lblEncoding = new Label();
        cboEncoding = new ComboBox();
        btnClear = new Button();
        btnSaveLog = new Button();

        splitMain = new SplitContainer();
        grpReceive = new GroupBox();
        txtReceive = new RichTextBox();
        panelSend = new Panel();
        txtSend = new TextBox();
        panelSendOptions = new FlowLayoutPanel();
        chkHexSend = new CheckBox();
        chkAppendNewLine = new CheckBox();
        chkAutoSend = new CheckBox();
        lblInterval = new Label();
        numInterval = new NumericUpDown();
        btnSend = new Button();
        btnClearSend = new Button();

        statusStrip = new StatusStrip();
        lblPortState = new ToolStripStatusLabel();
        lblRx = new ToolStripStatusLabel();
        lblTx = new ToolStripStatusLabel();
        lblTips = new ToolStripStatusLabel();
        tmrStatus = new System.Windows.Forms.Timer(components);
        tmrAutoSend = new System.Windows.Forms.Timer(components);

        // SuspendLayout / BeginInit 表示“先别计算布局”，等所有控件都加完再统一算，
        // 否则每加一个控件都要重排一次，界面会闪、启动也慢。
        panelParams.SuspendLayout();
        panelOptions.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)splitMain).BeginInit();
        splitMain.Panel1.SuspendLayout();
        splitMain.Panel2.SuspendLayout();
        splitMain.SuspendLayout();
        grpReceive.SuspendLayout();
        panelSend.SuspendLayout();
        panelSendOptions.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)numInterval).BeginInit();
        statusStrip.SuspendLayout();
        SuspendLayout();

        // ---------- 串口参数行 ----------
        panelParams.Dock = DockStyle.Top;
        panelParams.Height = 42;
        panelParams.Padding = new Padding(8, 8, 8, 0);
        panelParams.WrapContents = false;

        lblPort.Text = "串口";
        lblPort.AutoSize = true;
        lblPort.Margin = new Padding(0, 6, 4, 0);

        cboPort.DropDownStyle = ComboBoxStyle.DropDown;
        cboPort.Width = 100;

        btnRefreshPorts.Text = "刷新";
        btnRefreshPorts.Width = 56;
        btnRefreshPorts.Click += btnRefreshPorts_Click;

        lblBaud.Text = "波特率";
        lblBaud.AutoSize = true;
        lblBaud.Margin = new Padding(10, 6, 4, 0);

        cboBaud.DropDownStyle = ComboBoxStyle.DropDownList;
        cboBaud.Width = 90;

        lblDataBits.Text = "数据位";
        lblDataBits.AutoSize = true;
        lblDataBits.Margin = new Padding(10, 6, 4, 0);

        cboDataBits.DropDownStyle = ComboBoxStyle.DropDownList;
        cboDataBits.Width = 50;

        lblParity.Text = "校验";
        lblParity.AutoSize = true;
        lblParity.Margin = new Padding(10, 6, 4, 0);

        cboParity.DropDownStyle = ComboBoxStyle.DropDownList;
        cboParity.Width = 76;

        lblStopBits.Text = "停止位";
        lblStopBits.AutoSize = true;
        lblStopBits.Margin = new Padding(10, 6, 4, 0);

        cboStopBits.DropDownStyle = ComboBoxStyle.DropDownList;
        cboStopBits.Width = 56;

        btnOpen.Text = "打开串口";
        btnOpen.Width = 88;
        btnOpen.Height = 28;
        btnOpen.Margin = new Padding(14, 3, 0, 0);
        btnOpen.Click += btnOpen_Click;

        panelParams.Controls.AddRange(new Control[]
        {
            lblPort, cboPort, btnRefreshPorts, lblBaud, cboBaud, lblDataBits, cboDataBits,
            lblParity, cboParity, lblStopBits, cboStopBits, btnOpen
        });

        // ---------- 显示选项行 ----------
        panelOptions.Dock = DockStyle.Top;
        panelOptions.Height = 38;
        panelOptions.Padding = new Padding(8, 4, 8, 0);
        panelOptions.WrapContents = false;

        chkHexDisplay.Text = "HEX 显示";
        chkHexDisplay.AutoSize = true;
        chkHexDisplay.Margin = new Padding(0, 6, 8, 0);

        chkTimestamp.Text = "时间戳";
        chkTimestamp.AutoSize = true;
        chkTimestamp.Checked = true;
        chkTimestamp.Margin = new Padding(0, 6, 8, 0);

        chkAutoScroll.Text = "自动滚动";
        chkAutoScroll.AutoSize = true;
        chkAutoScroll.Checked = true;
        chkAutoScroll.Margin = new Padding(0, 6, 8, 0);

        chkPause.Text = "暂停显示";
        chkPause.AutoSize = true;
        chkPause.Margin = new Padding(0, 6, 8, 0);

        lblEncoding.Text = "编码";
        lblEncoding.AutoSize = true;
        lblEncoding.Margin = new Padding(8, 6, 4, 0);

        cboEncoding.DropDownStyle = ComboBoxStyle.DropDownList;
        cboEncoding.Width = 78;

        btnClear.Text = "清空接收";
        btnClear.Width = 78;
        btnClear.Click += btnClear_Click;

        btnSaveLog.Text = "保存日志";
        btnSaveLog.Width = 78;
        btnSaveLog.Click += btnSaveLog_Click;

        panelOptions.Controls.AddRange(new Control[]
        {
            chkHexDisplay, chkTimestamp, chkAutoScroll, chkPause,
            lblEncoding, cboEncoding, btnClear, btnSaveLog
        });

        // ---------- 接收区 ----------
        splitMain.Dock = DockStyle.Fill;
        splitMain.Orientation = Orientation.Horizontal;
        splitMain.SplitterWidth = 6;
        splitMain.Panel1MinSize = 150;
        splitMain.Panel2MinSize = 120;

        grpReceive.Dock = DockStyle.Fill;
        grpReceive.Text = "接收数据";
        grpReceive.Padding = new Padding(6);

        txtReceive.Dock = DockStyle.Fill;
        txtReceive.ReadOnly = true;
        txtReceive.BackColor = Color.FromArgb(24, 26, 30);
        txtReceive.ForeColor = Color.FromArgb(0, 220, 130);
        txtReceive.Font = new Font("Consolas", 10F);
        txtReceive.BorderStyle = BorderStyle.None;
        txtReceive.WordWrap = true;

        grpReceive.Controls.Add(txtReceive);
        splitMain.Panel1.Controls.Add(grpReceive);

        // ---------- 发送区 ----------
        panelSend.Dock = DockStyle.Fill;
        panelSend.Padding = new Padding(6);

        txtSend.Dock = DockStyle.Fill;
        txtSend.Multiline = true;
        txtSend.ScrollBars = ScrollBars.Vertical;
        txtSend.Font = new Font("Consolas", 10F);
        txtSend.Text = "01 03 00 00 00 0A C5 CD";

        panelSendOptions.Dock = DockStyle.Bottom;
        panelSendOptions.Height = 40;
        panelSendOptions.Padding = new Padding(0, 4, 0, 4);
        panelSendOptions.WrapContents = false;

        chkHexSend.Text = "HEX 发送";
        chkHexSend.AutoSize = true;
        chkHexSend.Checked = true;
        chkHexSend.Margin = new Padding(0, 6, 8, 0);

        chkAppendNewLine.Text = "追加 \\r\\n";
        chkAppendNewLine.AutoSize = true;
        chkAppendNewLine.Margin = new Padding(0, 6, 8, 0);

        chkAutoSend.Text = "定时发送";
        chkAutoSend.AutoSize = true;
        chkAutoSend.Margin = new Padding(0, 6, 4, 0);
        chkAutoSend.CheckedChanged += chkAutoSend_CheckedChanged;

        lblInterval.Text = "间隔(ms)";
        lblInterval.AutoSize = true;
        lblInterval.Margin = new Padding(8, 6, 4, 0);

        numInterval.Minimum = 50;
        numInterval.Maximum = 600000;
        numInterval.Value = 1000;
        numInterval.Increment = 50;
        numInterval.Width = 76;

        btnSend.Text = "发送 (Ctrl+Enter)";
        btnSend.Width = 130;
        btnSend.Height = 28;
        btnSend.Margin = new Padding(14, 2, 0, 0);
        btnSend.Click += btnSend_Click;

        btnClearSend.Text = "清空发送";
        btnClearSend.Width = 78;
        btnClearSend.Margin = new Padding(8, 2, 0, 0);
        btnClearSend.Click += btnClearSend_Click;

        panelSendOptions.Controls.AddRange(new Control[]
        {
            chkHexSend, chkAppendNewLine, chkAutoSend, lblInterval, numInterval, btnSend, btnClearSend
        });

        panelSend.Controls.Add(txtSend);
        panelSend.Controls.Add(panelSendOptions);
        splitMain.Panel2.Controls.Add(panelSend);
        splitMain.SplitterDistance = 380;

        // ---------- 状态栏 ----------
        statusStrip.Items.AddRange(new ToolStripItem[] { lblPortState, lblRx, lblTx, lblTips });
        lblPortState.Text = "未打开";
        lblPortState.ForeColor = Color.Firebrick;
        lblPortState.BorderSides = ToolStripStatusLabelBorderSides.Right;
        lblRx.Text = "收 0 字节";
        lblRx.BorderSides = ToolStripStatusLabelBorderSides.Right;
        lblTx.Text = "发 0 字节";
        lblTx.BorderSides = ToolStripStatusLabelBorderSides.Right;
        lblTips.Spring = true;
        lblTips.TextAlign = ContentAlignment.MiddleRight;
        lblTips.Text = "提示：Ctrl+Enter 发送；本工具支持 HEX/文本、定时发送";

        tmrStatus.Interval = 300;
        tmrStatus.Tick += tmrStatus_Tick;

        tmrAutoSend.Interval = 1000;
        tmrAutoSend.Tick += tmrAutoSend_Tick;

        // ---------- 窗体 ----------
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1000, 680);
        MinimumSize = new Size(820, 560);
        Text = "串口调试助手 · 上位机学习项目 01";
        StartPosition = FormStartPosition.CenterScreen;
        KeyPreview = true;
        KeyDown += MainForm_KeyDown;
        FormClosing += MainForm_FormClosing;
        Load += MainForm_Load;

        // Dock 布局的关键：后 Add 的控件先占位置。
        // 这里先加 splitMain（Fill 填满剩余区域），再加状态栏（Bottom）、
        // 选项行和参数行（Top），所以最终效果是“顶部两行 + 底部状态栏 + 中间自适应”。
        Controls.Add(splitMain);
        Controls.Add(statusStrip);
        Controls.Add(panelOptions);
        Controls.Add(panelParams);

        panelParams.ResumeLayout(false);
        panelParams.PerformLayout();
        panelOptions.ResumeLayout(false);
        panelOptions.PerformLayout();
        splitMain.Panel1.ResumeLayout(false);
        splitMain.Panel2.ResumeLayout(false);
        splitMain.Panel2.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)splitMain).EndInit();
        splitMain.ResumeLayout(false);
        grpReceive.ResumeLayout(false);
        panelSend.ResumeLayout(false);
        panelSend.PerformLayout();
        panelSendOptions.ResumeLayout(false);
        panelSendOptions.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)numInterval).EndInit();
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    // ---- 串口参数区 ----
    private FlowLayoutPanel panelParams;
    private Label lblPort;
    private ComboBox cboPort;
    private Button btnRefreshPorts;
    private Label lblBaud;
    private ComboBox cboBaud;
    private Label lblDataBits;
    private ComboBox cboDataBits;
    private Label lblParity;
    private ComboBox cboParity;
    private Label lblStopBits;
    private ComboBox cboStopBits;
    private Button btnOpen;

    // ---- 显示选项区 ----
    private FlowLayoutPanel panelOptions;
    private CheckBox chkHexDisplay;
    private CheckBox chkTimestamp;
    private CheckBox chkAutoScroll;
    private CheckBox chkPause;
    private Label lblEncoding;
    private ComboBox cboEncoding;
    private Button btnClear;
    private Button btnSaveLog;

    // ---- 接收区 / 发送区 ----
    private SplitContainer splitMain;
    private GroupBox grpReceive;
    private RichTextBox txtReceive;
    private Panel panelSend;
    private TextBox txtSend;
    private FlowLayoutPanel panelSendOptions;
    private CheckBox chkHexSend;
    private CheckBox chkAppendNewLine;
    private CheckBox chkAutoSend;
    private Label lblInterval;
    private NumericUpDown numInterval;
    private Button btnSend;
    private Button btnClearSend;

    // ---- 状态栏与定时器 ----
    private StatusStrip statusStrip;
    private ToolStripStatusLabel lblPortState;
    private ToolStripStatusLabel lblRx;
    private ToolStripStatusLabel lblTx;
    private ToolStripStatusLabel lblTips;
    private System.Windows.Forms.Timer tmrStatus;
    private System.Windows.Forms.Timer tmrAutoSend;
}
