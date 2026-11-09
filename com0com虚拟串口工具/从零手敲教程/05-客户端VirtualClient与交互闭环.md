# 05 · 客户端 VirtualClient 与交互闭环（里程碑 M5）

> **本章目标**：让窗口里的两个终端真的连到服务端、双向收发、自动应答，`MainForm.cs` 不用再改。
> **验收**：点「① 启动服务」→ VCOM2 自动显示「在线」；VCOM1 点「连接」；两个面板互发能看到 `TX →` / `RX ←`；勾「自动应答」能回包。

---

## 1. 客户端和服务端有什么不同

| | 服务端 `PairHub` | 客户端 `VirtualClient` |
|---|---|---|
| 谁发起连接 | 被动监听 `AcceptTcpClientAsync` | 主动 `ConnectAsync` |
| 握手 | 读第一行、回 `+OK`/`-ERR` | 发第一行、读应答 |
| 生命周期 | 一个实例服务 N 个连接 | 一个实例 = 一条连接 |
| 读数据 | `await stream.ReadAsync(..., token)` | 后台 Task 里**同步阻塞** `stream.Read` |
| 超时控制 | `CancellationTokenSource`（`ReadAsync` 不认 `ReadTimeout`） | `stream.ReadTimeout = 3000`（同步 `Read` 认它） |
| 关闭 | `Stop()` 取消所有会话 | `Disconnect()` 置标志 + Dispose |

> 这个对比很值得记：**`NetworkStream.ReadAsync` 不受 `ReadTimeout` 约束**，必须用取消令牌；
> 同步 `Read` 才认 `ReadTimeout`。两边写法不同是有原因的，不是随便写的。

---

## 2. 新建 `VirtualClient.cs`（最终版）

```csharp
using System.Net.Sockets;
using System.Text;

namespace VComPair;

/// <summary>
/// 虚拟串口客户端：工具自带的两个终端用它连到 PairHub，
/// 与串口调试助手里的 VirtualSerialPort 走完全相同的握手协议。
/// </summary>
public sealed class VirtualClient : IDisposable
{
    public event Action<byte[]>? Received;
    public event Action<string>? Log;
    public event Action? Disconnected;

    private readonly object _writeLock = new();
    private TcpClient? _client;
    private NetworkStream? _stream;
    private CancellationTokenSource? _cts;
    private volatile bool _closing;

    public string Endpoint { get; }
    public string Tag { get; }
    public bool IsConnected => !_closing && _client?.Connected == true && _stream is not null;

    public VirtualClient(string endpoint, string tag)
    {
        Endpoint = endpoint;
        Tag = tag;
    }

    public void Connect(string host, int port)
    {
        if (IsConnected) return;
        _closing = false;

        var client = new TcpClient { NoDelay = true };
        NetworkStream stream;
        try
        {
            try
            {
                if (!client.ConnectAsync(host, port).Wait(1500))
                    throw new IOException("连接超时：服务没有响应，请先点『启动服务』");
            }
            catch (AggregateException ex)
            {
                throw new IOException($"无法连接 127.0.0.1:{port}：请先点『启动服务』", ex.InnerException ?? ex);
            }

            stream = client.GetStream();
            stream.ReadTimeout = 3000;
            var hello = Encoding.UTF8.GetBytes($"HELLO|{Endpoint}|{Tag}\n");
            stream.Write(hello, 0, hello.Length);
            stream.Flush();

            var reply = ReadLine(stream);
            if (!reply.StartsWith("+OK", StringComparison.Ordinal))
                throw new IOException(reply.StartsWith("-ERR|", StringComparison.Ordinal) ? reply[5..] : reply);

            stream.ReadTimeout = Timeout.Infinite;
        }
        catch
        {
            client.Dispose();
            throw;
        }

        _client = client;
        _stream = stream;
        _cts = new CancellationTokenSource();
        _ = Task.Run(ReadLoop);
        Log?.Invoke($"[{DateTime.Now:HH:mm:ss.fff}] {Endpoint} 已连接（{Tag}）");
    }

    public void Send(byte[] data)
    {
        var stream = _stream;
        if (!IsConnected || stream is null)
            throw new IOException($"{Endpoint} 未连接，无法发送。");
        lock (_writeLock)
        {
            stream.Write(data, 0, data.Length);
            stream.Flush();
        }
    }

    public void Disconnect()
    {
        if (_closing && _client is null) return;
        _closing = true;

        var cts = _cts;
        var stream = _stream;
        var client = _client;
        _cts = null;
        _stream = null;
        _client = null;

        try { cts?.Cancel(); } catch { }
        try { stream?.Dispose(); } catch { }
        try { client?.Dispose(); } catch { }
        try { cts?.Dispose(); } catch { }

        Log?.Invoke($"[{DateTime.Now:HH:mm:ss.fff}] {Endpoint} 已断开");
        Disconnected?.Invoke();
    }

    private void ReadLoop()
    {
        var stream = _stream;
        if (stream is null) return;
        var buffer = new byte[4096];
        try
        {
            while (!_closing && stream is not null)
            {
                var n = stream.Read(buffer, 0, buffer.Length);
                if (n == 0) break;
                var frame = new byte[n];
                Buffer.BlockCopy(buffer, 0, frame, 0, n);
                Received?.Invoke(frame);
            }
        }
        catch (IOException) { }
        catch (ObjectDisposedException) { }
        catch (Exception ex) { Log?.Invoke($"[{DateTime.Now:HH:mm:ss.fff}] {Endpoint} 接收异常：{ex.Message}"); }
        finally
        {
            // 服务端主动关闭时才需要通知界面；自己点"断开"时 _closing 已为 true
            if (!_closing) Disconnect();
        }
    }

    private static string ReadLine(NetworkStream stream)
    {
        var bytes = new List<byte>();
        var one = new byte[1];
        while (bytes.Count < 1024)
        {
            var n = stream.Read(one, 0, 1);
            if (n == 0) break;
            if (one[0] == (byte)'\n') break;
            bytes.Add(one[0]);
        }
        while (bytes.Count > 0 && bytes[^1] == (byte)'\r') bytes.RemoveAt(bytes.Count - 1);
        return Encoding.UTF8.GetString(bytes.ToArray()).Trim();
    }

    public void Dispose() => Disconnect();
}
```

### 逐段讲解

| 片段 | 说明 |
|---|---|
| `ConnectAsync(...).Wait(1500)` | 给连接加超时，服务没启动时不会傻等；`Wait` 返回 `false` 即超时 |
| `catch (AggregateException ex)` | `Task.Wait()` 失败时抛的是**包装异常**，真原因在 `ex.InnerException`，必须拆开才能给出准确提示 |
| `stream.ReadTimeout = 3000` | 只影响**同步** `ReadLine`；握手阶段防恶意连接不发包 |
| `stream.ReadTimeout = Timeout.Infinite` | 握手完成，进入长连接接收模式，不要再 3 秒就超时 |
| `catch { client.Dispose(); throw; }` | 连接/握手任一步失败都要释放 socket，否则端口句柄泄漏 |
| `_ = Task.Run(ReadLoop)` | 后台阻塞读，绝不能放在 UI 线程 |
| `ReadLoop` 里复制 `frame` | 同服务端：`buffer` 复用，交给事件前必须复制 |
| `_closing` 标志 | 区分"我主动断开"和"服务端关了连接"。前者不用在 `finally` 里再触发 `Disconnect()`，避免重复事件 |
| `lock (_writeLock)` | UI 线程发送 + 后台线程自动应答可能并发写，加写锁防字节交错 |
| `IsConnected` | 三个条件同时满足：没在关闭、socket 还连着、stream 存在 |

### 三个事件的去向

```
VirtualClient.Received      → TerminalPanel.OnReceived  → Ui(BeginInvoke) → 接收区显示 RX ←
VirtualClient.Log           → TerminalPanel.Log → MainForm.AppendLog → 监控窗口
VirtualClient.Disconnected  → TerminalPanel.SetDisconnected → 按钮/状态回到「离线」
```

---

## 3. `TerminalPanel.cs` 整体替换（最终版）

把第 02 章的骨架**整体替换**为下面这份。界面部分（`BuildUi`）与骨架完全一致，
变化集中在：新增 `VirtualClient` 字段、订阅事件、把占位方法换成真实收发、新增 `AppendLine` / `Ui`。

```csharp
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
```

### 相比 M2 骨架，改了哪 6 处

| # | 改动 |
|---|---|
| 1 | 新增 `using System.Text;`（文本解码） |
| 2 | 新增 `private readonly VirtualClient _client`，构造函数里 `new VirtualClient(endpoint, "内置终端")` 并订阅 3 个事件 |
| 3 | `Endpoint` 改成 `=> _client.Endpoint`，去掉骨架里的自动属性 |
| 4 | `ConnectTo` / `DisconnectClient` / 新增 `ConnectInternal`：真实连接、失败弹中文提示 |
| 5 | `SendCurrent` 从弹窗自检改成 `BuildBytes → _client.Send → AppendLine("TX → ...")` |
| 6 | 新增 `OnReceived`（`RX ←` + 自动应答）、`AppendLine`（限流+滚动）、`Ui`（切 UI 线程） |

---

## 4. 交互闭环全链路（把前面几章串起来）

```
【启动】
UI: 点「① 启动服务」
  → MainForm.StartHub()
  → new PairHub(9527) + 订阅 Log/Traffic/StatusChanged
  → hub.Start()  → 后台 AcceptLoop
  → _terminals[VCOM2].ConnectTo()
      → VirtualClient.Connect → HELLO|VCOM2|内置终端 → +OK
      → 后台 ReadLoop 启动
  → 状态栏：VCOM2 已连接 · 内置终端

【发送】
UI: VCOM2 面板输入 "PING" 点「发送」
  → TerminalPanel.SendCurrent
  → ProtocolHelper.BuildBytes → UTF8 4 字节
  → VirtualClient.Send → socket.Write
  → AppendLine("[time] TX → PING")            ← 本端立刻回显

【服务端转发】
后台: PairHub.HandleClientAsync 读到 4 字节
  → Buffer.BlockCopy 复制成 frame
  → Deliver(VCOM2 → VCOM1)
  → Traffic 事件 ──► MainForm.Ui(BeginInvoke) ──► 监控窗口打印流向
  → VCOM1 没连 → 缓存 1 帧 + EmitLog

【接收】
UI: VCOM1 面板点「连接」 → +OK → ReadLoop 启动
  → 服务端 FlushPending 补发缓存的 4 字节
  → VirtualClient.ReadLoop 读到 → Received 事件（后台线程）
  → TerminalPanel.OnReceived → Ui(BeginInvoke) → AppendLine("[time] RX ← PING")

【自动应答】
VCOM1 勾「自动应答」+ 发送框填 "ACK"
  → OnReceived 里 _client.Send("ACK") → 回到服务端 → 转给 VCOM2 → VCOM2 显示 RX ← ACK

【断开】
UI: 点「断开」或「停止服务」
  → Disconnect 置 _closing、Dispose socket → Disconnected 事件 → SetDisconnected
  → 服务端读到 n==0 → DropSession → StatusChanged → 状态栏刷新
```

---

## 5. 验收清单

- [ ] `Ctrl+Shift+B` 0 错误 0 警告
- [ ] 点「① 启动服务」→ VCOM2 面板自动变「在线」，VCOM1 仍「离线」
- [ ] VCOM1 点「连接」→ 变「在线」；监控窗口打印"补发缓存的 1 帧"（如果之前发过）
- [ ] VCOM1 发 `Hello` → VCOM2 收到 `RX ← Hello`，两个面板都有 `TX →` 回显
- [ ] 勾 VCOM2 的「HEX 显」再发 `01 03 00 0A` → 收到 `01 03 00 0A`
- [ ] 勾「自动应答」→ 一端发帧，另一端自动回一帧
- [ ] 勾「回环模式」→ 发送端自己收到自己发的帧
- [ ] 点「断开」→ 状态回「离线」，再次「连接」能成功
- [ ] 点「停止服务」→ 两个面板都回「离线」，无残留进程
- [ ] 能把第 4 节的完整链路口头讲一遍，并指出哪几步在后台线程

---

**下一章**：[06 · 与串口助手集成](06-与串口助手集成.md) —— 让本仓库的串口调试助手选 `VCOM1` 也能收发，完成真正的上位机通讯。
