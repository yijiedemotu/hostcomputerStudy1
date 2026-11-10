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