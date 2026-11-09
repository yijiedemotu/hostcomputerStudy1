# 04 · 服务端 PairHub 逐行实现（里程碑 M4）

> **本章目标**：把 `PairHub.cs` 骨架换成真正的 TCP 服务端，让「① 启动服务」真的能监听 9527；
> 然后用 **PowerShell 手工客户端**验证握手与转发（不用写客户端就能测协议）。
> **验收**：点「① 启动服务」→ 监控窗口打印"服务已启动"；PowerShell 两个连接分别握手成功、A 发的字节 B 能收到。

---

## 1. 服务端到底要干几件事

| # | 职责 | 对应方法 |
|---|---|---|
| 1 | 监听 `127.0.0.1:9527`，接受连接 | `Start()` / `AcceptLoopAsync()` |
| 2 | 每个连接读第一行握手、登记到会话表 | `HandleClientAsync()` |
| 3 | 一个端点只允许一个连接（端口独占） | `_sessions` 字典 + `lock` |
| 4 | 握手后的字节原样转给对端 | `Deliver()` |
| 5 | 对端没连时先缓存，上线补发 | `_pending` 队列 + `FlushPending()` |
| 6 | 回环模式：数据发回本端 | `Loopback` 属性 |
| 7 | 把日志 / 每帧流向 / 状态变化通知界面 | `Log` / `Traffic` / `StatusChanged` 事件 |
| 8 | 停服务时干净断开所有连接 | `Stop()` / `DropSession()` |

### 线程模型（本章最重要的概念）

```
UI 线程 ── 点「① 启动服务」──► hub.Start()（立刻返回，不阻塞界面）
                                   │
                                   ├─ Task A: AcceptLoopAsync   无限等待新连接
                                   ├─ Task B: HandleClientAsync  VCOM1 连接，阻塞读
                                   └─ Task C: HandleClientAsync  VCOM2 连接，阻塞读

后台 Task ── Log / Traffic / StatusChanged 事件 ──► MainForm.Ui() ──► BeginInvoke ──► UI 线程改控件
```

> **铁律**：WinForms 控件只能在创建它的 UI 线程访问。所有服务端回调都在后台线程，
> 必须经 `Ui(...)` → `BeginInvoke` 切回 UI 线程，否则界面不更新，甚至随机崩溃。

---

## 2. `PairHub.cs` 整体替换（最终版，一次敲定）

把第 02 章的骨架**整体替换**为下面这份（约 350 行，建议分几次敲，每敲完一段先编译一次）：

```csharp
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace VComPair;

/// <summary>
/// 虚拟串口对服务端：一根"用 TCP 回环实现的空调制解调器线"。
///
///   VCOM1 写 ──► 服务 ──► VCOM2 读
///   VCOM2 写 ──► 服务 ──► VCOM1 读
///
/// 为什么不直接造 COM 口？Windows 上真正的 COM 口必须由内核驱动（com0com 就是干这个的）
/// 注册设备对象，纯托管代码做不到，而且需要管理员权限 + 驱动签名。
/// 这里用本机 TCP 回环（127.0.0.1）模拟"线"，客户端只需要能连 socket：
///   * 本仓库的串口调试助手 → Industrial.Core/Transport/VirtualSerialPort.cs
///   * 本工具自带的两个终端   → VirtualClient.cs
/// 因此零驱动、零管理员权限、零第三方依赖，装上 .NET 8 就能通讯。
///
/// 握手协议（UTF-8 一行，\n 结束；中文提示必须 UTF-8，ASCII 会变成 ?）：
///   客户端：HELLO|VCOM1|SerialAssistant
///   服务端：+OK|VCOM1      或  -ERR|原因
/// 握手之后的字节全部当作"串口上的裸数据"透明转发，不做任何分包。
/// </summary>
public sealed class PairHub : IDisposable
{
    public const string EndA = "VCOM1";
    public const string EndB = "VCOM2";
    public static readonly string[] Endpoints = [EndA, EndB];

    private readonly int _port;
    private readonly object _gate = new();
    private readonly Dictionary<string, Session> _sessions = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Queue<byte[]>> _pending = new(StringComparer.OrdinalIgnoreCase)
    {
        [EndA] = new(),
        [EndB] = new()
    };

    private TcpListener? _listener;
    private CancellationTokenSource? _acceptCts;
    private volatile bool _running;
    private volatile bool _loopback;

    /// <summary>文本日志（服务启停、连接/断开、缓存补发）。回调在后台线程，UI 需自行切线程。</summary>
    public event Action<string>? Log;

    /// <summary>每一帧的流向：(来源端点, 目标端点, 原始字节)。</summary>
    public event Action<string, string, byte[]>? Traffic;

    /// <summary>端点占用状态变化。</summary>
    public event Action? StatusChanged;

    public PairHub(int port) => _port = port;

    public bool IsRunning => _running;
    public int Port => _port;

    /// <summary>回环模式：数据发给自己而不是对端，用于单端自测。</summary>
    public bool Loopback
    {
        get => _loopback;
        set
        {
            _loopback = value;
            EmitLog(value ? "已开启回环模式：数据原样返回本端" : "已关闭回环模式：VCOM1 ↔ VCOM2 互联");
        }
    }

    /// <summary>查询某个端点当前被谁占用。</summary>
    public string EndpointState(string endpoint)
    {
        lock (_gate)
            return _sessions.TryGetValue(endpoint, out var s) ? $"已连接 · {s.Tag}" : "空闲（可被外部程序占用）";
    }

    public void Start()
    {
        if (_running) return;

        _listener = new TcpListener(IPAddress.Loopback, _port);
        _listener.Start(); // 端口被占用会抛 SocketException，交给界面提示
        var cts = new CancellationTokenSource();
        _acceptCts = cts;
        _running = true;

        EmitLog($"服务已启动：127.0.0.1:{_port}（{EndA} ↔ {EndB}）");
        _ = Task.Run(() => AcceptLoopAsync(cts));
        StatusChanged?.Invoke();
    }

    public void Stop()
    {
        if (!_running) return;
        _running = false;

        try { _acceptCts?.Cancel(); } catch { }
        try { _listener?.Stop(); } catch { }

        List<Session> sessions;
        lock (_gate)
        {
            sessions = _sessions.Values.ToList();
            _sessions.Clear();
            _pending[EndA].Clear();
            _pending[EndB].Clear();
        }
        foreach (var s in sessions) DropSession(s, false);

        EmitLog("服务已停止，所有连接已断开");
        StatusChanged?.Invoke();
    }

    private async Task AcceptLoopAsync(CancellationTokenSource cts)
    {
        try
        {
            while (!cts.IsCancellationRequested && _listener is not null)
            {
                TcpClient client;
                try
                {
                    client = await _listener.AcceptTcpClientAsync(cts.Token);
                }
                catch (OperationCanceledException) { break; }
                catch (ObjectDisposedException) { break; }
                catch (SocketException) { break; }
                catch (Exception ex) { EmitLog($"监听异常：{ex.Message}"); break; }

                _ = Task.Run(() => HandleClientAsync(client));
            }
        }
        finally
        {
            // 只释放本次启动持有的 CTS，绝不动字段：避免"停止→立刻再启动"时把新 CTS 清空
            cts.Dispose();
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        Session? session = null;
        try
        {
            client.NoDelay = true;
            var stream = client.GetStream();

            var hello = await ReadLineAsync(stream);
            var parts = hello.Split('|', 3);
            if (parts.Length < 2 || !string.Equals(parts[0].Trim(), "HELLO", StringComparison.OrdinalIgnoreCase))
            {
                await SendControlAsync(stream, "-ERR|握手协议错误：请使用 VComPair 客户端或本仓库的串口调试助手");
                return;
            }

            var endpoint = parts[1].Trim().ToUpperInvariant();
            if (endpoint != EndA && endpoint != EndB)
            {
                await SendControlAsync(stream, $"-ERR|未知端点 {endpoint}，只允许 {EndA} / {EndB}");
                return;
            }

            var tag = parts.Length > 2 && parts[2].Trim().Length > 0 ? parts[2].Trim() : "外部程序";
            var candidate = new Session(endpoint, tag, client, stream);

            lock (_gate)
            {
                // 和真实串口一样：一个端口同时只能被一个程序打开
                if (!_sessions.ContainsKey(endpoint))
                {
                    _sessions[endpoint] = candidate;
                    session = candidate;
                }
            }

            if (session is null)
            {
                await SendControlAsync(stream, $"-ERR|{endpoint} 已被占用（同一串口只能被一个程序打开，请先断开旧连接）");
                return;
            }

            await SendControlAsync(stream, $"+OK|{endpoint}");
            EmitLog($"{endpoint} 已连接：{tag}");
            StatusChanged?.Invoke();
            FlushPending(session);

            var buffer = new byte[4096];
            while (_running && !session.Cts.IsCancellationRequested)
            {
                int n;
                try
                {
                    n = await stream.ReadAsync(buffer, 0, buffer.Length, session.Cts.Token);
                }
                catch (OperationCanceledException) { break; }
                catch (IOException) { break; }
                catch (ObjectDisposedException) { break; }
                if (n == 0) break;

                var frame = new byte[n];
                Buffer.BlockCopy(buffer, 0, frame, 0, n);
                Deliver(session, frame);
            }
        }
        catch (Exception ex)
        {
            EmitLog($"连接处理异常：{ex.Message}");
        }
        finally
        {
            if (session is not null) DropSession(session, true);
            else
            {
                try { client.Dispose(); } catch { }
            }
        }
    }

    /// <summary>把一帧送到对端（或回环模式下的本端）。对端不在线时先缓存，上线后补发。</summary>
    private void Deliver(Session from, byte[] data)
    {
        var to = _loopback ? from.Endpoint : (from.Endpoint == EndA ? EndB : EndA);
        Traffic?.Invoke(from.Endpoint, to, data);

        Session? target;
        lock (_gate) _sessions.TryGetValue(to, out target);

        if (target is null)
        {
            lock (_gate)
            {
                var queue = _pending[to];
                if (queue.Count >= 128) queue.Dequeue(); // 最多缓存 128 帧，防止无限增长
                queue.Enqueue(data);
            }
            EmitLog($"{to} 未连接，已缓存 1 帧（{data.Length} 字节），待其上线后补发");
            return;
        }

        try
        {
            lock (target.WriteLock)
            {
                target.Stream.Write(data, 0, data.Length);
                target.Stream.Flush();
            }
        }
        catch (Exception ex)
        {
            EmitLog($"{to} 写入失败：{ex.Message}，连接已断开");
            DropSession(target, true);
        }
    }

    private void FlushPending(Session session)
    {
        byte[][] frames;
        lock (_gate)
        {
            frames = _pending[session.Endpoint].ToArray();
            _pending[session.Endpoint].Clear();
        }
        if (frames.Length == 0) return;

        try
        {
            lock (session.WriteLock)
            {
                foreach (var f in frames) session.Stream.Write(f, 0, f.Length);
                session.Stream.Flush();
            }
            EmitLog($"{session.Endpoint} 上线，补发缓存的 {frames.Length} 帧 / {frames.Sum(f => f.Length)} 字节");
        }
        catch (Exception ex)
        {
            EmitLog($"补发缓存失败：{ex.Message}");
        }
    }

    private void DropSession(Session s, bool notify)
    {
        if (Interlocked.Exchange(ref s.Dropped, 1) != 0) return; // 避免 Stop 与 finally 重复处理

        lock (_gate)
        {
            if (_sessions.TryGetValue(s.Endpoint, out var current) && ReferenceEquals(current, s))
                _sessions.Remove(s.Endpoint);
        }

        try { s.Cts.Cancel(); } catch { }
        try { s.Stream.Dispose(); } catch { }
        try { s.Client.Dispose(); } catch { }
        try { s.Cts.Dispose(); } catch { }

        if (notify)
        {
            EmitLog($"{s.Endpoint} 已断开：{s.Tag}");
            StatusChanged?.Invoke();
        }
    }

    private void EmitLog(string message)
    {
        try { Log?.Invoke($"[{DateTime.Now:HH:mm:ss.fff}] {message}"); }
        catch { /* 界面已关闭时忽略 */ }
    }

    /// <summary>读一行 UTF-8 握手信息，5 秒没读到就放弃，防止异常连接把线程挂死。</summary>
    private static async Task<string> ReadLineAsync(NetworkStream stream)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var bytes = new List<byte>();
        var one = new byte[1];
        while (bytes.Count < 1024)
        {
            var n = await stream.ReadAsync(one.AsMemory(0, 1), cts.Token);
            if (n == 0) break;
            if (one[0] == (byte)'\n') break;
            bytes.Add(one[0]);
        }
        while (bytes.Count > 0 && bytes[^1] == (byte)'\r') bytes.RemoveAt(bytes.Count - 1);
        return Encoding.UTF8.GetString(bytes.ToArray()).Trim();
    }

    private static async Task SendControlAsync(NetworkStream stream, string text)
    {
        // 错误原因里可能有中文（例如"端口已被占用"），必须用 UTF-8 而不是 ASCII
        var bytes = Encoding.UTF8.GetBytes(text + "\n");
        await stream.WriteAsync(bytes, 0, bytes.Length);
        await stream.FlushAsync();
    }

    public void Dispose() => Stop();

    private sealed class Session
    {
        public readonly string Endpoint;
        public readonly string Tag;
        public readonly TcpClient Client;
        public readonly NetworkStream Stream;
        public readonly CancellationTokenSource Cts = new();
        public readonly object WriteLock = new();
        public int Dropped;

        public Session(string endpoint, string tag, TcpClient client, NetworkStream stream)
        {
            Endpoint = endpoint;
            Tag = tag;
            Client = client;
            Stream = stream;
        }
    }
}
```

---

## 3. 逐段讲解

### 3.1 字段

| 字段 | 作用 |
|---|---|
| `_gate` | 一把锁，保护 `_sessions` 和 `_pending` 两个共享字典（后台线程会并发读写） |
| `_sessions` | 端点名 → 当前唯一会话。`OrdinalIgnoreCase` 让 `vcom1` 也能匹配 |
| `_pending` | 每个端点一个离线缓存队列（最多 128 帧，防止无限增长） |
| `_listener` | `TcpListener`，只绑 `IPAddress.Loopback`（**不对外网开放**） |
| `_acceptCts` | 取消令牌，停止服务时让 `AcceptTcpClientAsync` 立刻退出 |
| `volatile bool` | `volatile` 保证多线程读到的值是最新的，不会因为寄存器缓存看到旧值 |

### 3.2 `Start()` / `Stop()`

```csharp
_listener = new TcpListener(IPAddress.Loopback, _port);
_listener.Start();     // 端口被占用 → 抛 SocketException
_running = true;
_ = Task.Run(() => AcceptLoopAsync(cts));
```

- `_ = Task.Run(...)` 里的 `_ =` 是"我故意不等这个任务"的写法（丢弃返回值），避免编译器警告；
- `AcceptLoopAsync` 里 `await AcceptTcpClientAsync(cts.Token)` 是**异步等待**，不占线程；
- `Stop()` 先 `Cancel()` 再 `Stop()` listener，然后取出所有会话逐个 `DropSession`，最后发日志/状态事件。

> `EmitLog` 里用 `try/catch` 包住事件调用：窗口关闭后事件订阅者可能已释放，不能让后台线程把整个服务带崩。

### 3.3 握手 `HandleClientAsync`

```csharp
var hello = await ReadLineAsync(stream);
var parts = hello.Split('|', 3);          // 最多切 3 段，标签里就算有 | 也不会炸
```

- 校验命令字 `HELLO`（忽略大小写）；
- 端点名 `ToUpperInvariant()` 归一化；
- `lock (_gate)` 里检查并登记会话 —— **必须在锁内完成"检查 + 写入"**，否则两个连接可能同时通过检查，破坏"独占"；
- 被占用就回中文 `-ERR` 并 `return`（`finally` 里 `session is null` 分支负责 `client.Dispose()`）；
- 成功后回 `+OK`，触发 `StatusChanged`，再 `FlushPending()` 补发缓存。

### 3.4 读循环

```csharp
n = await stream.ReadAsync(buffer, 0, buffer.Length, session.Cts.Token);
if (n == 0) break;                       // 对端正常关闭
var frame = new byte[n];
Buffer.BlockCopy(buffer, 0, frame, 0, n); // 必须复制！buffer 下一轮会被覆盖
Deliver(session, frame);
```

**为什么必须复制**：`buffer` 是每个连接复用的数组，如果直接把 `buffer` 交给事件/队列，下一轮 `Read` 会覆盖内容，接收端读到错数据。这是网络编程最经典的坑之一。

### 3.5 `Deliver()` 转发

```csharp
var to = _loopback ? from.Endpoint : (from.Endpoint == EndA ? EndB : EndA);
Traffic?.Invoke(...);          // 先通知监控窗口
lock (_gate) _sessions.TryGetValue(to, out target);
if (target is null) { 缓存; return; }
lock (target.WriteLock) { target.Stream.Write(...); }
```

- `WriteLock` 防止两个线程同时往同一个 socket 写导致字节交错；
- 写失败就 `DropSession` 清理会话，不留僵尸连接。

### 3.6 `DropSession()` 幂等

```csharp
if (Interlocked.Exchange(ref s.Dropped, 1) != 0) return;
```

`Stop()` 和 `HandleClientAsync` 的 `finally` 都可能触发清理。
`Interlocked.Exchange` 是**原子操作**，保证只执行一次，避免重复 `Dispose` / 重复日志。

### 3.7 `ReadLineAsync` 的两个细节

1. `CancellationTokenSource(TimeSpan.FromSeconds(5))` —— 客户端连上却一直不发握手，5 秒后自动放弃，防止线程被挂死；
   **注意**：`NetworkStream.ReadAsync` 不理会 `ReadTimeout`，必须用取消令牌控制超时。
2. 按**字节**收集到 `List<byte>`，最后一次性 `Encoding.UTF8.GetString(...)` —— 中文才不会变 `?`。

### 3.8 `Session` 内部类

一个会话持有：端点名、来源标签、`TcpClient`、`NetworkStream`、自己的取消令牌、写锁、清理标记。
把"一条连接"封装成对象，比散落一堆并行数组清晰得多。

---

## 4. `MainForm.cs` 的三处替换

> 改完这三处，`MainForm.cs` 就**定稿**了，第 05 章不用再动它。

### 替换 1：加字段

在 `public sealed class MainForm : Form` 的 `{` 之后加一行：

```csharp
    private PairHub? _hub;
```

### 替换 2：构造函数末尾

```csharp
        BuildUi();
        FormClosing += (_, _) => Shutdown();   // ← 新增这一行
```

### 替换 3：从「占位事件绑定」到文件末尾整体替换

在 `MainForm.cs` 里找到这一行注释：

```csharp
        // ↓↓↓ 第 04 章会替换这一整块事件绑定
```

**从这一行开始，一直到文件末尾（包括最后的 `}`），整体替换**为：

```csharp
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
```

### 讲解

| 片段 | 说明 |
|---|---|
| `hub.Log += AppendLog` | 服务端日志（后台线程）→ `AppendLog` 用 `Ui()` 切成 UI 线程再写监控窗口 |
| `hub.Traffic += ...` | 每帧流向 → 格式化成 `时间 来源→目标 字节数 \| HEX \| ASCII` |
| `Ui(Action)` | `BeginInvoke` 是**异步**排队，不阻塞后台线程；`IsHandleCreated` 检查避免窗口还没创建时抛异常 |
| `?.` / `!` | `_hub?.` 可空安全访问；`_hub!` 是"我确定这里非 null"的编译期声明 |
| `port is < 1 or > 65535` | 关系模式匹配校验端口范围 |
| `FormClosing → Shutdown()` | 关窗口时停服务、断开连接，不留后台进程 |
| `AppendLog` 从 M2 的直接调用改成 `Ui(...)` | 因为现在回调来自后台线程了 |

> 现在 `_terminals[PairHub.EndB].ConnectTo()` 仍会弹第 02 章的占位提示 —— 正常，第 05 章替换 `TerminalPanel` 后就会真的连上。

---

## 5. PowerShell 手工验证（本章最爽的一步）

**不需要写客户端**，用 Windows 自带 PowerShell 就能验证协议。

F5 启动工具 → 点「① 启动服务」→ 新建 PowerShell 窗口，粘贴：

```powershell
$enc = [System.Text.Encoding]::UTF8

function Handshake($client, $ep) {
    $s = $client.GetStream()
    $line = $enc.GetBytes("HELLO|$ep|手工验证`n")
    $s.Write($line, 0, $line.Length); $s.Flush()
    $buf = [byte[]]::new(256)
    $n = $s.Read($buf, 0, $buf.Length)
    Write-Host "  $ep 握手应答: $($enc.GetString($buf, 0, $n).Trim())"
    return $s
}

Write-Host "① 建立两个连接"
$a = [System.Net.Sockets.TcpClient]::new('127.0.0.1', 9527)
$b = [System.Net.Sockets.TcpClient]::new('127.0.0.1', 9527)

Write-Host "② 握手"
$sa = Handshake $a 'VCOM1'
$sb = Handshake $b 'VCOM2'

Write-Host "③ VCOM1 发一帧"
$msg = $enc.GetBytes('PING from VCOM1')
$sa.Write($msg, 0, $msg.Length); $sa.Flush()

$buf = [byte[]]::new(256)
$n = $sb.Read($buf, 0, $buf.Length)
Write-Host "④ VCOM2 收到: $($enc.GetString($buf, 0, $n))"

Write-Host "⑤ 反向再发一帧"
$msg2 = $enc.GetBytes('PONG from VCOM2')
$sb.Write($msg2, 0, $msg2.Length); $sb.Flush()
$n = $sa.Read($buf, 0, $buf.Length)
Write-Host "⑥ VCOM1 收到: $($enc.GetString($buf, 0, $n))"

$sa.Dispose(); $sb.Dispose()
```

### 预期输出

```
① 建立两个连接
② 握手
  VCOM1 握手应答: +OK|VCOM1
  VCOM2 握手应答: +OK|VCOM2
③ VCOM1 发一帧
④ VCOM2 收到: PING from VCOM1
⑤ 反向再发一帧
⑥ VCOM1 收到: PONG from VCOM2
```

监控窗口同步打印两条 `VCOM1→VCOM2 ... | PING from VCOM1` 之类的流向。

### 再试 4 个边界实验

| 实验 | 怎么做 | 预期 |
|---|---|---|
| 独占 | 保持上面连接，再开一个 PowerShell 连 `HELLO\|VCOM1\|第三个` | 收到 `-ERR\|VCOM1 已被占用（…中文完整…）` |
| 未知端点 | 发 `HELLO\|VCOM9\|x` | `-ERR\|未知端点 VCOM9` |
| 错误命令 | 发 `GET / HTTP/1.1` | `-ERR\|握手协议错误…` |
| 离线缓存 | 只连 VCOM1 就发一帧，再连 VCOM2 握手 | 监控先打印"已缓存 1 帧"，VCOM2 上线后收到补发 |

> 「中文完整」这条正好验证第 03 章讲的 UTF-8：如果服务端的 `SendControlAsync` 用了 `Encoding.ASCII`，
> 这里会收到一串 `??????`。

---

## 6. 验收清单

- [ ] `Ctrl+Shift+B` 0 错误 0 警告
- [ ] 点「① 启动服务」→ 状态变 `● 服务运行中`，监控窗口打印"服务已启动：127.0.0.1:9527"
- [ ] 点「停止服务」→ 状态回 `○ 服务未启动`，监控打印"服务已停止"
- [ ] PowerShell 脚本 6 步输出全部符合预期
- [ ] 独占 / 未知端点 / 错误命令 / 离线缓存 四个边界实验通过
- [ ] 端口改成 `abc` 或 `99999` 点启动 → 弹"端口不合法"，不崩溃
- [ ] 勾「回环模式」后 VCOM1 发的数据回到 VCOM1 自己
- [ ] 能说清为什么 `ReadAsync` 后必须 `Buffer.BlockCopy` 复制一份
- [ ] 能说清 `Ui()` → `BeginInvoke` 解决了什么问题

---

**下一章**：[05 · 客户端 VirtualClient 与交互闭环](05-客户端VirtualClient与交互闭环.md) —— 让两个内置终端真的连上、互发、自动应答。
