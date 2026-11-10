using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace VComPair;

/// <summary>
/// 一个"端点终端"面板：连接/断开、HEX 与文本收发、自动应答。
/// VCOM1 与 VCOM2 各一块，合起来就是一对虚拟串口。
///
/// 布局只用 Dock（Top / Bottom / Right / Fill），不用 TableLayoutPanel：
/// 嵌套 TableLayoutPanel 若忘记给单列容器设 ColumnStyles(Percent 100)，
/// 列宽会按 AutoSize 塌缩成内容最小宽度，控件会挤成一团。Dock 布局没有这个坑。
/// 注意添加顺序：Fill 控件第一个 Add，边缘控件后 Add（WinForms 按 z-order 反向停靠）。
/// </summary>
internal sealed class TerminalPanel : GroupBox
{
    public event Action<string>? Log;

    private readonly VirtualClient _client;
    private readonly Label _state = new();
    private readonly Button _btnConnect = new();
    private readonly CheckBox _hexSend = new();
    private readonly CheckBox _hexDisplay = new();
    private readonly CheckBox _autoReply = new();
    private readonly Button _btnClear = new();
    private readonly RichTextBox _recv = new();
    private readonly TextBox _send = new();
    private readonly Button _btnSend = new();

    private string _host = "127.0.0.1";
    private int _port = 9527;

    public string Endpoint => _client.Endpoint;
    public VirtualClient Client => _client;

    public TerminalPanel(string endpoint, string title, string defaultText)
    {
        Text = title;
        Padding = new Padding(6);
        _client = new VirtualClient(endpoint, "内置终端");
        _client.Received += OnReceived;
        _client.Log += s => Log?.Invoke(s);
        _client.Disconnected += () => Ui(SetDisconnected);
        BuildUi(defaultText);
    }

    public void SetTarget(string host, int port)
    {
        _host = host;
        _port = port;
    }

    public void ConnectTo() => ConnectInternal();

    public void DisconnectClient()
    {
        _client.Disconnect();
        if (!IsDisposed) SetDisconnected();
    }

    private void ConnectInternal()
    {
        try
        {
            _client.Connect(_host, _port);
            _btnConnect.Text = "断开";
            _state.Text = "在线";
            _state.ForeColor = Color.SeaGreen;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"{Endpoint} 连接失败：{ex.Message}", "虚拟串口",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void BuildUi(string defaultText)
    {
        // ---------- 顶部选项条 ----------
        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 34,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(2, 2, 2, 0)
        };

        // 标签用短文本 + ToolTip，保证窗口缩到最小宽度时选项条仍能单行放下
        _state.Text = "离线";
        _state.AutoSize = true;
        _state.ForeColor = Color.Firebrick;
        _state.Margin = new Padding(2, 8, 3, 0);

        _btnConnect.Text = "连接";
        _btnConnect.Width = 56;
        _btnConnect.Height = 26;
        _btnConnect.Margin = new Padding(0, 2, 2, 2);

        _hexSend.Text = "HEX 发";
        _hexSend.AutoSize = true;
        _hexSend.Margin = new Padding(4, 8, 2, 0);

        _hexDisplay.Text = "HEX 显";
        _hexDisplay.AutoSize = true;
        _hexDisplay.Margin = new Padding(3, 8, 2, 0);

        _autoReply.Text = "自动应答";
        _autoReply.AutoSize = true;
        _autoReply.Margin = new Padding(3, 8, 2, 0);

        _btnClear.Text = "清空";
        _btnClear.Width = 62;
        _btnClear.Height = 26;
        _btnClear.Margin = new Padding(4, 2, 0, 2);

        var tips = new ToolTip();
        tips.SetToolTip(_hexSend, "按十六进制解析发送框内容，例如 01 03 00 0A");
        tips.SetToolTip(_hexDisplay, "接收区按十六进制显示");
        tips.SetToolTip(_autoReply, "收到数据后用发送框内容自动回一帧，可模拟仪表/从站");

        bar.Controls.AddRange(new Control[]
            { _state, _btnConnect, _hexSend, _hexDisplay, _autoReply, _btnClear });

        // ---------- 底部发送行：文本框 Fill + 发送按钮 Right ----------
        var sendRow = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 70,
            Padding = new Padding(0, 4, 0, 0)
        };

        _send.Dock = DockStyle.Fill;
        _send.Multiline = true;
        _send.ScrollBars = ScrollBars.Vertical;
        _send.Text = defaultText;

        _btnSend.Dock = DockStyle.Right;
        _btnSend.Width = 84;
        _btnSend.Text = "发送";
        _btnSend.Margin = new Padding(4, 2, 0, 2);

        sendRow.Controls.Add(_send);    // Fill 先添加
        sendRow.Controls.Add(_btnSend); // Right 后添加

        // ---------- 中间接收区 ----------
        _recv.Dock = DockStyle.Fill;
        _recv.ReadOnly = true;
        _recv.BackColor = Color.White;
        _recv.Font = new Font("Consolas", 9F);
        _recv.WordWrap = false;
        _recv.ScrollBars = RichTextBoxScrollBars.Vertical;

        Controls.Add(_recv);     // Fill
        Controls.Add(sendRow);   // Bottom
        Controls.Add(bar);       // Top

        _btnConnect.Click += (_, _) =>
        {
            if (_client.IsConnected) DisconnectClient();
            else ConnectInternal();
        };
        _btnSend.Click += (_, _) => SendCurrent();
        _btnClear.Click += (_, _) => _recv.Clear();
    }

    private void SendCurrent()
    {
        try
        {
            var data = ProtocolHelper.BuildBytes(_send.Text, _hexSend.Checked);
            if (data.Length == 0) return;
            _client.Send(data);

            // 本端回显发送内容，界面上立刻能看到"我发了什么"，对照对端更直观
            AppendLine($"[{DateTime.Now:HH:mm:ss.fff}] TX → " + (_hexSend.Checked
                ? ProtocolHelper.ToHex(data)
                : Encoding.UTF8.GetString(data)));
        }
        catch (FormatException ex)
        {
            MessageBox.Show(this, ex.Message, "发送内容格式错误",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "发送失败",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void OnReceived(byte[] data)
    {
        // 接收回调在后台线程，所有控件访问都必须切回 UI 线程
        Ui(() =>
        {
            var line = $"[{DateTime.Now:HH:mm:ss.fff}] RX ← " + (_hexDisplay.Checked
                ? ProtocolHelper.ToHex(data)
                : Encoding.UTF8.GetString(data).Replace("\r", string.Empty));
            AppendLine(line);

            // 自动应答：用发送框里的内容当作设备回包，可模拟仪表/Modbus 从站
            if (_autoReply.Checked && _send.TextLength > 0 && _client.IsConnected)
            {
                try
                {
                    var reply = ProtocolHelper.BuildBytes(_send.Text, _hexSend.Checked);
                    if (reply.Length > 0)
                    {
                        _client.Send(reply);
                        AppendLine($"[{DateTime.Now:HH:mm:ss.fff}] TX → (自动应答) " + (_hexSend.Checked
                            ? ProtocolHelper.ToHex(reply)
                            : Encoding.UTF8.GetString(reply)));
                    }
                }
                catch (Exception ex)
                {
                    Log?.Invoke($"[{DateTime.Now:HH:mm:ss.fff}] 自动应答失败：{ex.Message}");
                }
            }
        });
    }

    /// <summary>追加一行到接收区，并做长度限流 + 自动滚动。调用方必须已在 UI 线程。</summary>
    private void AppendLine(string line)
    {
        _recv.AppendText(line + Environment.NewLine);
        if (_recv.TextLength > 200_000)
        {
            _recv.Select(0, 100_000);
            _recv.SelectedText = string.Empty;
        }
        _recv.SelectionStart = _recv.TextLength;
        _recv.ScrollToCaret();
    }

    private void SetDisconnected()
    {
        if (IsDisposed) return;
        _btnConnect.Text = "连接";
        _state.Text = "离线";
        _state.ForeColor = Color.Firebrick;
    }

    private void Ui(Action action)
    {
        if (IsDisposed || !IsHandleCreated) return;
        try { BeginInvoke(action); } catch { }
    }
}