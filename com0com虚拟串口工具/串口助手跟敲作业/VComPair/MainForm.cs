using System.Drawing;
using System.Windows.Forms;

namespace VComPair;

/// <summary>
/// 虚拟串口对工具主窗口：
///   ① 在本机建立 VCOM1 ↔ VCOM2 的一根"虚拟空调制解调器线"；
///   ② 自带两个终端，其中 VCOM2 默认连上，作为串口调试助手的对端；
///   ③ 数据监控窗口把每一帧的流向、HEX、ASCII 都打印出来。
///
/// 布局方案：Dock + SplitContainer（不用 TableLayoutPanel）。
/// 之前用嵌套 TableLayoutPanel 时，单列容器没设 ColumnStyles(Percent 100)，
/// 列宽按 AutoSize 塌缩，控件全部挤到左上角。Dock 布局不会塌缩，且能随窗口缩放。
/// 添加顺序很重要：Fill 控件第一个 Add，Top/Bottom 控件后 Add。
/// </summary>
public sealed class MainForm : Form
{
    private readonly TextBox _txtPort = new();
    private readonly Button _btnStart = new();
    private readonly Button _btnStop = new();
    private readonly CheckBox _chkLoopback = new();
    private readonly Button _btnClear = new();
    private readonly Label _lblHubState = new();
    private readonly RichTextBox _monitor = new();
    private readonly Label _lblStatus = new();
    private readonly Dictionary<string, TerminalPanel> _terminals = new();
    private PairHub? _hub;

    public MainForm()
    {
        Text = "虚拟串口对工具 VComPair · 免驱动 COM 通讯";
        Size = new Size(1120, 720);
        MinimumSize = new Size(920, 560);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Microsoft YaHei UI", 9F);
        BuildUi();
        FormClosing += (_, _) => Shutdown();   // ← 新增这一行
    }

    private void BuildUi()
    {
        // ================= 顶部工具条 =================
        var top = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 48,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(4, 4, 4, 0)
        };

        // 固定宽度避免"启动后文字变长"把整排控件挤开；窄窗口下也能单行放下
        _lblHubState.Text = "○ 服务未启动";
        _lblHubState.AutoSize = false;
        _lblHubState.Width = 130;
        _lblHubState.ForeColor = Color.Firebrick;
        _lblHubState.Margin = new Padding(2, 10, 8, 0);

        var lblPort = new Label { Text = "端口", AutoSize = true, Margin = new Padding(2, 10, 4, 0) };

        _txtPort.Text = "9527";
        _txtPort.Width = 68;
        _txtPort.Margin = new Padding(0, 6, 8, 0);

        // 按钮宽度留足余量，否则中文会被 WinForms 裁掉一两个字
        _btnStart.Text = "① 启动服务";
        _btnStart.Width = 120;
        _btnStart.Height = 28;
        _btnStart.Margin = new Padding(2);

        _btnStop.Text = "停止服务";
        _btnStop.Width = 100;
        _btnStop.Height = 28;
        _btnStop.Enabled = false;
        _btnStop.Margin = new Padding(2);

        _chkLoopback.Text = "回环模式";
        _chkLoopback.AutoSize = true;
        _chkLoopback.Margin = new Padding(8, 10, 2, 0);

        _btnClear.Text = "清空监控";
        _btnClear.Width = 100;
        _btnClear.Height = 28;
        _btnClear.Margin = new Padding(8, 2, 2, 2);

        var btnHelp = new Button
        {
            Text = "使用说明",
            Width = 100,
            Height = 28,
            Margin = new Padding(2)
        };

        var tips = new ToolTip();
        tips.SetToolTip(_chkLoopback, "回环模式（自发自收）：数据发回本端，用于单端自测");
        tips.SetToolTip(btnHelp, "使用说明");
        top.Controls.AddRange(new Control[]
        {
            _lblHubState, lblPort, _txtPort, _btnStart, _btnStop, _chkLoopback, _btnClear, btnHelp
        });

        // ================= 底部状态栏 =================
        _lblStatus.Dock = DockStyle.Bottom;
        _lblStatus.Height = 26;
        _lblStatus.AutoSize = false;
        _lblStatus.AutoEllipsis = true;
        _lblStatus.TextAlign = ContentAlignment.MiddleLeft;
        _lblStatus.ForeColor = Color.DimGray;
        _lblStatus.Padding = new Padding(6, 0, 6, 0);

        // ================= 主体：左右 SplitContainer =================
        // 注意：不要在构造时设置 Panel1MinSize / Panel2MinSize —— 此时控件还没有实际尺寸，
        // SplitContainer 会立刻校验 SplitterDistance 并抛 InvalidOperationException。
        // 使用默认的 25px 最小值，在 Shown 里再按窗口尺寸设置分栏位置。
        var mainSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 6
        };

        // 左侧：两个终端上下对半分
        var leftSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterWidth = 6
        };

        var t1 = new TerminalPanel(PairHub.EndA,
            "VCOM1 · 外部串口助手（默认不占用）", "Hello VCOM1!");
        var t2 = new TerminalPanel(PairHub.EndB,
            "VCOM2 · 内置对端（启动服务后自动连接）", "Hello VCOM2!");
        // 放进 SplitterPanel 必须显式 Dock=Fill，否则 GroupBox 保持默认 200x100，
        // 内部接收区会被压成 0 高（TableLayoutPanel 会自动做，SplitContainer 不会）
        t1.Dock = DockStyle.Fill;
        t2.Dock = DockStyle.Fill;
        _terminals[PairHub.EndA] = t1;
        _terminals[PairHub.EndB] = t2;
        t1.Log += AppendLog;
        t2.Log += AppendLog;

        leftSplit.Panel1.Controls.Add(t1);
        leftSplit.Panel2.Controls.Add(t2);
        mainSplit.Panel1.Controls.Add(leftSplit);

        // 右侧：数据监控 Fill + 操作步骤 Bottom
        var gbMonitor = new GroupBox
        {
            Text = "数据监控 · 每一帧的流向",
            Dock = DockStyle.Fill,
            Padding = new Padding(8)
        };
        _monitor.Dock = DockStyle.Fill;
        _monitor.ReadOnly = true;
        _monitor.BackColor = Color.White;
        _monitor.Font = new Font("Consolas", 9F);
        _monitor.WordWrap = true;   // 长日志自动换行，而不是在右侧被裁掉
        _monitor.ScrollBars = RichTextBoxScrollBars.Vertical;
        gbMonitor.Controls.Add(_monitor);

        var gbHelp = new GroupBox
        {
            Text = "操作步骤（第一次用请照着做）",
            Dock = DockStyle.Bottom,
            Height = 196,
            Padding = new Padding(8)
        };
        var help = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            Text =
                "1. 点「① 启动服务」→ 建立 VCOM1 ↔ VCOM2 虚拟线。\r\n" +
                "2. 另开终端：dotnet run --project 项目/src/SerialAssistant\r\n" +
                "3. 串口助手端口选 VCOM1 → 点「打开串口」。\r\n" +
                "4. VCOM2 面板点「发送」→ 助手立刻收到，反向同理。\r\n" +
                "5. 两终端互测：VCOM1 也点「连接」（外部助手先断开）。\r\n" +
                "6. 勾「自动应答」：用发送框内容回一帧，模拟仪表。\r\n" +
                "注：VCOM 不是 COMx；只认 COM 口的软件请用 com0com（02/03 号教程）。"
        };
        gbHelp.Controls.Add(help);

        mainSplit.Panel2.Controls.Add(gbMonitor); // Fill 先添加
        mainSplit.Panel2.Controls.Add(gbHelp);     // Bottom 后添加

        Controls.Add(mainSplit);   // Fill 先添加（索引 0，最后停靠，占满剩余空间）
        Controls.Add(_lblStatus);  // Bottom
        Controls.Add(top);         // Top

        // SplitterDistance 必须在控件有实际尺寸之后设置，否则会抛异常
        Shown += (_, _) =>
        {
            try
            {
                // SplitterDistance 合法区间约为 [25, Width - 25 - SplitterWidth]，这里留足余量
                var d = (int)(mainSplit.Width * 0.58);
                d = Math.Clamp(d, 60, Math.Max(60, mainSplit.Width - 60));
                mainSplit.SplitterDistance = d;

                var h = leftSplit.Height / 2;
                h = Math.Clamp(h, 60, Math.Max(60, leftSplit.Height - 60));
                leftSplit.SplitterDistance = h;
            }
            catch { /* 极端窗口尺寸下忽略，保持默认分栏 */ }
        };

        _btnStart.Click += (_, _) => StartHub();
        _btnStop.Click += (_, _) => StopHub();
        _btnClear.Click += (_, _) => _monitor.Clear();
        _chkLoopback.CheckedChanged += (_, _) =>
        {
            if (_hub is not null) _hub.Loopback = _chkLoopback.Checked;
        };
        btnHelp.Click += (_, _) => MessageBox.Show(this,
            "VComPair 是免驱动的虚拟串口对工具。\r\n\r\n" +
            "它用本机 TCP 回环模拟一根空调制解调器线，两个端点叫 VCOM1 / VCOM2。\r\n" +
            "本仓库的串口调试助手已经内置 VCOM 客户端，选择 VCOM1 就能收发。\r\n\r\n" +
            "如果你的软件只接受设备管理器里的 COM5/COM6 这类名字，\r\n" +
            "那需要真正的内核驱动 com0com，见本文件夹的 02 / 03 / 05 号教程。",
            "使用说明", MessageBoxButtons.OK, MessageBoxIcon.Information);

        RefreshStatus();
    }

    private void StartHub()
    {
        if (_hub?.IsRunning == true) return;
        if (!int.TryParse(_txtPort.Text.Trim(), out var port) || port is < 1 or > 65535)
        {
            MessageBox.Show(this, "监听端口请填 1~65535 的整数（默认 9527）。", "端口不合法",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var hub = new PairHub(port);
            hub.Log += AppendLog;
            hub.Traffic += (from, to, data) => Ui(() => AppendMonitor(FormatTraffic(from, to, data)));
            hub.StatusChanged += () => Ui(RefreshStatus);
            hub.Start();
            _hub = hub;

            foreach (var t in _terminals.Values) t.SetTarget("127.0.0.1", port);
            _btnStart.Enabled = false;
            _btnStop.Enabled = true;
            _txtPort.Enabled = false;
            RefreshStatus();

            AppendMonitor("──── 服务已启动：VCOM1 ↔ VCOM2 ────");
            AppendMonitor("外部串口调试助手选 VCOM1 → 点『打开串口』");
            AppendMonitor("VCOM2 面板作为对端，点『发送』即可互传");

            // 默认占用 VCOM2 作为"设备侧"，VCOM1 留给外部串口助手
            _terminals[PairHub.EndB].ConnectTo();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                $"启动失败：{ex.Message}\r\n\r\n端口被占用时改一个端口（例如 9528）再点『启动服务』。",
                "启动失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void StopHub()
    {
        foreach (var t in _terminals.Values) t.DisconnectClient();
        _hub?.Stop();
        _hub?.Dispose();
        _hub = null;

        _btnStart.Enabled = true;
        _btnStop.Enabled = false;
        _txtPort.Enabled = true;
        RefreshStatus();
        AppendMonitor("──── 服务已停止 ────");
    }

    private void Shutdown()
    {
        foreach (var t in _terminals.Values) t.DisconnectClient();
        _hub?.Stop();
        _hub?.Dispose();
        _hub = null;
    }

    private void RefreshStatus()
    {
        var running = _hub?.IsRunning == true;
        _lblHubState.Text = running ? "● 服务运行中" : "○ 服务未启动";
        _lblHubState.ForeColor = running ? Color.SeaGreen : Color.Firebrick;

        var a = running ? _hub!.EndpointState(PairHub.EndA) : "服务未启动";
        var b = running ? _hub!.EndpointState(PairHub.EndB) : "服务未启动";
        _lblStatus.Text = $"VCOM1：{a}    |    VCOM2：{b}    |    外部串口助手选 VCOM1，内置对端占 VCOM2";
    }

    private void AppendLog(string message) => Ui(() => AppendMonitor(message));

    private void AppendMonitor(string line)
    {
        if (IsDisposed) return;
        _monitor.AppendText(line + Environment.NewLine);
        if (_monitor.TextLength > 200_000)
        {
            _monitor.Select(0, 100_000);
            _monitor.SelectedText = string.Empty;
        }
        _monitor.SelectionStart = _monitor.TextLength;
        _monitor.ScrollToCaret();
    }

    private static string FormatTraffic(string from, string to, byte[] data) =>
        $"[{DateTime.Now:HH:mm:ss.fff}] {from}→{to} {data.Length}B | " +
        $"{ProtocolHelper.ToHex(data)} | {ProtocolHelper.ToAscii(data)}";

    private void Ui(Action action)
    {
        if (IsDisposed || !IsHandleCreated) return;
        try { BeginInvoke(action); } catch { }
    }
}