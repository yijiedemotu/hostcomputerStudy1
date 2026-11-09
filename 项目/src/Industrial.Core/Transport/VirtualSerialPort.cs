using System.Net.Sockets;
using System.Text;

namespace Industrial.Core.Transport;

/// <summary>
/// 免驱动的虚拟串口客户端（配套工具：com0com虚拟串口工具/VComPair）。
///
/// 【为什么需要它】
/// 真正的虚拟 COM 口（COM5 ↔ COM6）必须由 Windows 内核驱动创建，com0com 就是这样一个驱动，
/// 安装需要管理员权限、且常因驱动签名在 Win10/11 上失败。
/// 本实现走另一条路：用本机 TCP 回环（127.0.0.1:9527）模拟"一根空调制解调器线"，
/// 端点叫 VCOM1 / VCOM2。它同样实现 <see cref="ISerialPort"/>，
/// 所以串口调试助手、Modbus RTU 主站等业务代码一行都不用改，换个实现就能通讯。
///
/// 【和真实串口的差异】
/// * 波特率/数据位/校验/停止位不参与传输（虚拟线没有电气层），但两端参数建议填成一样；
/// * 端口名是 VCOM1 / VCOM2，不会出现在设备管理器里；
/// * 需要一个"服务端"先启动，即 VComPair 工具；第三方只认 COMx 的软件请用真正的 com0com。
///
/// 握手：连接后先发一行 "HELLO|VCOM1|SerialAssistant\n"，服务端回 "+OK|VCOM1\n"；
/// 握手之后的字节全部是串口裸数据，不做分包、不做转义。
/// </summary>
public sealed class VirtualSerialPort : ISerialPort
{
    public const string DefaultHost = "127.0.0.1";
    public const int DefaultPort = 9527;

    /// <summary>可用的虚拟端点，与 VComPair 工具的 VCOM1 / VCOM2 对应。</summary>
    public static readonly string[] VirtualPortNames = ["VCOM1", "VCOM2"];

    private readonly string _portName;
    private readonly string _host;
    private readonly int _port;
    private readonly object _writeLock = new();

    private TcpClient? _client;
    private NetworkStream? _stream;
    private volatile bool _closing;

    public VirtualSerialPort(string portName, string host = DefaultHost, int port = DefaultPort)
    {
        if (!IsVirtualPort(portName))
            throw new ArgumentException(
                $"不是合法的虚拟串口名（应为 {string.Join(" / ", VirtualPortNames)}）：{portName}", nameof(portName));

        _portName = portName.Trim().ToUpperInvariant();
        _host = host;
        _port = port;
    }

    /// <summary>判断一个端口名是不是虚拟端口（大小写不敏感）。</summary>
    public static bool IsVirtualPort(string? name)
    {
        if (name is null) return false;
        var trimmed = name.Trim();
        foreach (var p in VirtualPortNames)
            if (string.Equals(p, trimmed, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    public string PortName => _portName;

    public bool IsOpen => !_closing && _client?.Connected == true && _stream is not null;

    public void Open()
    {
        if (IsOpen) return;
        _closing = false;

        var client = new TcpClient { NoDelay = true };
        try
        {
            try
            {
                if (!client.ConnectAsync(_host, _port).Wait(1500))
                    throw new IOException($"连接虚拟串口服务超时（{_host}:{_port}）：请先启动『虚拟串口对工具 VComPair』。");
            }
            catch (AggregateException ex)
            {
                throw new IOException(
                    $"连不上虚拟串口服务（{_host}:{_port}）：请先启动『虚拟串口对工具 VComPair』。",
                    ex.InnerException ?? ex);
            }

            var stream = client.GetStream();
            stream.ReadTimeout = 3000;

            var hello = Encoding.UTF8.GetBytes($"HELLO|{_portName}|SerialAssistant\n");
            stream.Write(hello, 0, hello.Length);
            stream.Flush();

            var reply = ReadLine(stream);
            if (!reply.StartsWith("+OK", StringComparison.Ordinal))
            {
                var reason = reply.StartsWith("-ERR|", StringComparison.Ordinal) ? reply[5..] : reply;
                throw new IOException($"虚拟串口 {_portName} 打开失败：{reason}");
            }

            stream.ReadTimeout = Timeout.Infinite;
            _client = client;
            _stream = stream;
        }
        catch (Exception ex) when (ex is not IOException)
        {
            client.Dispose();
            throw new IOException($"连接虚拟串口服务失败（{_host}:{_port}）：{ex.Message}", ex);
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    public void Close()
    {
        if (_closing && _client is null) return;
        _closing = true;

        var stream = _stream;
        var client = _client;
        _stream = null;
        _client = null;

        try { stream?.Dispose(); } catch { }
        try { client?.Dispose(); } catch { }
    }

    public void Write(byte[] data)
    {
        var stream = _stream;
        if (!IsOpen || stream is null)
            throw new InvalidOperationException("虚拟串口未打开。");

        try
        {
            lock (_writeLock)
            {
                stream.Write(data, 0, data.Length);
                stream.Flush();
            }
        }
        catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException)
        {
            throw new IOException("虚拟串口写入失败：对端工具可能已关闭或连接被断开。", ex);
        }
    }

    /// <summary>
    /// 带超时的阻塞读：用 DataAvailable 轮询而不是 socket 超时异常，
    /// 语义与 <see cref="SerialPortNative.Read"/> 完全一致（超时返回 0，不抛异常）。
    /// </summary>
    public int Read(byte[] buffer, int offset, int count, int timeoutMs)
    {
        var stream = _stream ?? throw new InvalidOperationException("虚拟串口未打开。");
        if (count <= 0) return 0;

        var deadline = Environment.TickCount64 + Math.Max(1, timeoutMs);
        while (true)
        {
            if (_closing || _client is null || !_client.Connected)
                throw new ObjectDisposedException(nameof(VirtualSerialPort));

            if (stream.DataAvailable)
                return stream.Read(buffer, offset, count);

            var remain = deadline - Environment.TickCount64;
            if (remain <= 0) return 0;
            Thread.Sleep((int)Math.Min(10, remain));
        }
    }

    public void DiscardBuffers()
    {
        var stream = _stream;
        if (stream is null) return;
        try
        {
            var tmp = new byte[4096];
            while (stream.DataAvailable) stream.Read(tmp, 0, tmp.Length);
        }
        catch { /* 连接已断开时忽略 */ }
    }

    public void Dispose() => Close();

    /// <summary>读一行 UTF-8 握手响应（握手阶段 stream.ReadTimeout = 3000ms）。</summary>
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
}
