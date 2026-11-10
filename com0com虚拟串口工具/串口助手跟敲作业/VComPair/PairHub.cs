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

    //`TcpListener`，只绑 `IPAddress.Loopback`（**不对外网开放**）
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