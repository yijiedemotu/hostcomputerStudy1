using System.Net.Sockets;

namespace Industrial.Core.Modbus;

/// <summary>
/// Modbus TCP 主站。
///
/// 帧结构 = MBAP 报文头(7 字节) + PDU：
///   [事务号 2][协议号 2 = 0][长度 2 = 单元号+PDU 字节数][单元号 1][功能码 1][数据 N]
/// 端口默认 502；一个连接上可以串行发多帧，用事务号匹配应答。
///
/// 本实现特点：
///  * 一个 SemaphoreSlim 保证“一问一答”串行，避免多线程并发把应答串包；
///  * 每帧有超时（默认 3 秒），超时抛 <see cref="ModbusTimeoutException"/>；
///  * 从站返回异常码时抛 <see cref="ModbusException"/>，由上层决定重试还是报警。
/// </summary>
public sealed class ModbusTcpMaster : IModbusMaster
{
    // 连接目标与超时时间，构造后不再改变
    private readonly string _host;
    private readonly int _port;
    private readonly int _timeoutMs;

    // 信号量初始为 1，相当于“同一时刻只允许一个人用这条连接”。
    // Modbus TCP 虽然可以并发，但很多小型 PLC/网关处理不了并发请求，
    // 串行化最稳妥，也避免两个请求的应答互相串包。
    private readonly SemaphoreSlim _gate = new(1, 1);

    private TcpClient? _client;
    private NetworkStream? _stream;

    // 事务号：每发一帧加 1，应答里必须带回同一个号，用来确认“这回的是我”
    private ushort _transactionId;

    public ModbusTcpMaster(string host, int port = 502, int timeoutMs = 3000)
    {
        _host = host;
        _port = port;
        _timeoutMs = timeoutMs;
    }

    public string Endpoint => $"{_host}:{_port}";

    public bool IsConnected => _client?.Connected == true && _stream is not null;

    /// <summary>
    /// 建立 TCP 连接。
    /// 这里把“用户取消”和“连接超时”区分开：用户主动取消就正常退出，
    /// 超过 _timeoutMs 才抛 ModbusTimeoutException，提示去查网线/IP/防火墙。
    /// </summary>
    public async Task ConnectAsync(CancellationToken ct = default)
    {
        Disconnect();

        // NoDelay = true 关闭 Nagle 算法，小帧立即发送，减少几十毫秒延迟
        var client = new TcpClient { NoDelay = true };

        // CreateLinkedTokenSource：把外部取消令牌和“超时”合并成一个令牌
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(_timeoutMs);
        try
        {
            await client.ConnectAsync(_host, _port, cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // 外部没取消，是我们自己等超时了
            client.Dispose();
            throw new ModbusTimeoutException($"连接 {Endpoint} 超时：检查网线、IP、防火墙与端口 502");
        }
        catch (SocketException ex)
        {
            // 网络层错误：目标不可达、连接被拒绝等
            client.Dispose();
            throw new IOException($"连接 {Endpoint} 失败：{ex.Message}", ex);
        }

        _client = client;
        _stream = client.GetStream();
        _stream.ReadTimeout = _timeoutMs;
    }

    /// <summary>断开连接。Dispose 会同时关掉流和 Socket，重复调用是安全的。</summary>
    public void Disconnect()
    {
        _stream?.Dispose();
        _client?.Dispose();
        _stream = null;
        _client = null;
    }

    public Task<bool[]> ReadCoilsAsync(byte unitId, ushort startAddress, ushort count, CancellationToken ct = default)
        => ReadBitsAsync(unitId, 0x01, startAddress, count, ct);

    public Task<bool[]> ReadDiscreteInputsAsync(byte unitId, ushort startAddress, ushort count, CancellationToken ct = default)
        => ReadBitsAsync(unitId, 0x02, startAddress, count, ct);

    /// <summary>
    /// 读保持寄存器（03）。请求 PDU 共 5 字节：功能码 + 起始地址(2) + 数量(2)。
    /// 应答 PDU：功能码 + 字节数 + 数据。每个寄存器 2 字节，高字节在前。
    /// </summary>
    public async Task<ushort[]> ReadHoldingRegistersAsync(byte unitId, ushort startAddress, ushort count, CancellationToken ct = default)
    {
        EnsureRegisterCount(count); // 一次最多读 125 个，Modbus 规范限制

        var pdu = new byte[5];
        pdu[0] = 0x03;
        WriteUInt16(pdu, 1, startAddress);
        WriteUInt16(pdu, 3, count);

        var body = await TransactAsync(unitId, pdu, ct).ConfigureAwait(false);

        // 先验证应答“长得对不对”，再解析数据，避免把错位的数据当温度
        if (body[0] != 0x03) throw new ModbusProtocolException($"应答功能码不符：0x{body[0]:X2}");
        var byteCount = body[1];
        if (byteCount != count * 2) throw new ModbusProtocolException($"字节数不符：期望 {count * 2}，实际 {byteCount}");

        var result = new ushort[count];
        for (var i = 0; i < count; i++)
            result[i] = (ushort)((body[2 + i * 2] << 8) | body[3 + i * 2]); // 两字节拼成一个寄存器
        return result;
    }

    /// <summary>读输入寄存器（04）。流程和 03 完全一样，只是功能码不同。</summary>
    public async Task<ushort[]> ReadInputRegistersAsync(byte unitId, ushort startAddress, ushort count, CancellationToken ct = default)
    {
        EnsureRegisterCount(count);
        var pdu = new byte[5];
        pdu[0] = 0x04;
        WriteUInt16(pdu, 1, startAddress);
        WriteUInt16(pdu, 3, count);

        var body = await TransactAsync(unitId, pdu, ct).ConfigureAwait(false);
        if (body[0] != 0x04) throw new ModbusProtocolException($"应答功能码不符：0x{body[0]:X2}");
        var byteCount = body[1];
        if (byteCount != count * 2) throw new ModbusProtocolException($"字节数不符：期望 {count * 2}，实际 {byteCount}");

        var result = new ushort[count];
        for (var i = 0; i < count; i++)
            result[i] = (ushort)((body[2 + i * 2] << 8) | body[3 + i * 2]);
        return result;
    }

    /// <summary>读线圈/离散输入（01/02）。每 8 个开关量打包成 1 个字节。</summary>
    private async Task<bool[]> ReadBitsAsync(byte unitId, byte function, ushort startAddress, ushort count, CancellationToken ct)
    {
        if (count is < 1 or > 2000)
            throw new ArgumentOutOfRangeException(nameof(count), "位读取数量必须在 1..2000");

        var pdu = new byte[5];
        pdu[0] = function;
        WriteUInt16(pdu, 1, startAddress);
        WriteUInt16(pdu, 3, count);

        var body = await TransactAsync(unitId, pdu, ct).ConfigureAwait(false);
        if (body[0] != function) throw new ModbusProtocolException($"应答功能码不符：0x{body[0]:X2}");

        // 向上取整：9 个位需要 (9+7)/8 = 2 个字节
        var expectedBytes = (count + 7) / 8;
        if (body[1] != expectedBytes) throw new ModbusProtocolException($"位应答字节数不符：期望 {expectedBytes}，实际 {body[1]}");

        var result = new bool[count];
        for (var i = 0; i < count; i++)
            // 第 i 位在 body[2 + i/8] 的第 i%8 位；& (1 << n) 取出那一位，非 0 就是 true
            result[i] = (body[2 + i / 8] & (1 << (i % 8))) != 0;
        return result;
    }

    /// <summary>写单个线圈（05）。注意“开”固定写 0xFF00，“关”写 0x0000，不是 1/0。</summary>
    public async Task WriteSingleCoilAsync(byte unitId, ushort address, bool value, CancellationToken ct = default)
    {
        var pdu = new byte[5];
        pdu[0] = 0x05;
        WriteUInt16(pdu, 1, address);
        WriteUInt16(pdu, 3, value ? (ushort)0xFF00 : (ushort)0x0000);

        var body = await TransactAsync(unitId, pdu, ct).ConfigureAwait(false);
        if (body[0] != 0x05) throw new ModbusProtocolException($"写线圈应答功能码不符：0x{body[0]:X2}");
    }

    /// <summary>写单个保持寄存器（06）。从站会原样返回请求，用来确认写入成功。</summary>
    public async Task WriteSingleRegisterAsync(byte unitId, ushort address, ushort value, CancellationToken ct = default)
    {
        var pdu = new byte[5];
        pdu[0] = 0x06;
        WriteUInt16(pdu, 1, address);
        WriteUInt16(pdu, 3, value);

        var body = await TransactAsync(unitId, pdu, ct).ConfigureAwait(false);
        if (body[0] != 0x06) throw new ModbusProtocolException($"写寄存器应答功能码不符：0x{body[0]:X2}");
    }

    /// <summary>
    /// 连续写多个寄存器（10 / 0x10）。
    /// 请求 PDU：功能码 + 起始地址(2) + 数量(2) + 字节数(1) + 数据(N*2)。
    /// </summary>
    public async Task WriteMultipleRegistersAsync(byte unitId, ushort startAddress, ushort[] values, CancellationToken ct = default)
    {
        if (values.Length is < 1 or > 123)
            throw new ArgumentOutOfRangeException(nameof(values), "一次最多写 123 个寄存器");

        var pdu = new byte[6 + values.Length * 2];
        pdu[0] = 0x10;
        WriteUInt16(pdu, 1, startAddress);
        WriteUInt16(pdu, 3, (ushort)values.Length);
        pdu[5] = (byte)(values.Length * 2); // 后面数据区一共多少字节
        for (var i = 0; i < values.Length; i++)
            WriteUInt16(pdu, 6 + i * 2, values[i]);

        var body = await TransactAsync(unitId, pdu, ct).ConfigureAwait(false);
        if (body[0] != 0x10) throw new ModbusProtocolException($"批量写应答功能码不符：0x{body[0]:X2}");
    }

    /// <summary>
    /// 发送一帧 PDU 并取回应答 PDU（不含 MBAP 头）。
    /// 这是整个 TCP 主站的核心：拼 MBAP 头 → 发送 → 先读 7 字节头 →
    /// 根据头里的长度再读剩余部分 → 检查异常码 → 返回 PDU。
    /// 整个方法用 _gate 包住，保证多线程下同一时刻只有一个请求在飞。
    /// </summary>
    private async Task<byte[]> TransactAsync(byte unitId, byte[] pdu, CancellationToken ct)
    {
        if (_stream is null) throw new InvalidOperationException("未连接，请先调用 ConnectAsync。");

        // 排队拿锁；如果前一个请求还没完成，这里会异步等待，不阻塞线程
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // 外部取消 + 本次请求超时，合并成一个令牌
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(_timeoutMs);

            // ---- 拼 MBAP 头（7 字节）----
            var tid = ++_transactionId;              // 事务号，每帧 +1
            var frame = new byte[7 + pdu.Length];
            frame[0] = (byte)(tid >> 8);             // 事务号高字节
            frame[1] = (byte)(tid & 0xFF);           // 事务号低字节
            frame[2] = 0;                            // 协议标识固定 0（Modbus）
            frame[3] = 0;
            var length = (ushort)(pdu.Length + 1);   // 长度 = 单元号(1) + PDU
            frame[4] = (byte)(length >> 8);
            frame[5] = (byte)(length & 0xFF);
            frame[6] = unitId;                       // 单元号/从站号
            pdu.CopyTo(frame, 7);                    // 把 PDU 接在头后面

            try
            {
                // ---- 发送 ----
                await _stream.WriteAsync(frame, timeoutCts.Token).ConfigureAwait(false);
                await _stream.FlushAsync(timeoutCts.Token).ConfigureAwait(false);

                // ---- 先读固定 7 字节的 MBAP 头 ----
                var header = await ReadExactAsync(7, timeoutCts.Token).ConfigureAwait(false);

                // 事务号必须和请求一致
                var respTid = (ushort)((header[0] << 8) | header[1]);
                if (respTid != tid)
                    throw new ModbusProtocolException($"事务号不匹配：发送 {tid}，收到 {respTid}（可能有别的程序在抢答）");

                // 长度字段告诉我们后面还有多少字节（含单元号）
                var respLength = (header[4] << 8) | header[5];
                if (respLength < 2 || respLength > 260)
                    throw new ModbusProtocolException($"MBAP 长度非法：{respLength}");

                // 头里已读了单元号，剩余 PDU 长度为 respLength - 1
                var body = await ReadExactAsync(respLength - 1, timeoutCts.Token).ConfigureAwait(false);

                // 功能码最高位为 1 表示异常应答
                if ((body[0] & 0x80) != 0)
                    throw new ModbusException((byte)(body[0] & 0x7F), body.Length > 1 ? body[1] : (byte)0);

                return body;
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                // 外部没取消，说明是请求超时
                throw new ModbusTimeoutException($"Modbus TCP 请求超时（{_timeoutMs}ms）：单元号 {unitId}，功能码 0x{pdu[0]:X2}。检查从站号、设备是否在线、是否有网关");
            }
            catch (IOException ex) when (ex.InnerException is SocketException se)
            {
                throw new IOException($"Modbus TCP 通信中断：{se.Message}", ex);
            }
        }
        finally
        {
            // 无论成功、超时还是异常，都必须释放信号量，否则下一次请求会永远卡住
            _gate.Release();
        }
    }

    /// <summary>
    /// 精确读取 count 个字节。
    /// TCP 是“流”，一次 Read 可能只收到半帧，所以必须循环读到够数为止。
    /// 返回 0 表示对端关闭了连接。
    /// </summary>
    private async Task<byte[]> ReadExactAsync(int count, CancellationToken ct)
    {
        var buffer = new byte[count];
        var offset = 0;
        while (offset < count)
        {
            var read = await _stream!.ReadAsync(buffer.AsMemory(offset, count - offset), ct).ConfigureAwait(false);
            if (read == 0)
                throw new ModbusProtocolException("连接被从站关闭（读到 0 字节）");
            offset += read;
        }
        return buffer;
    }

    /// <summary>把一个 ushort 写成两字节，高字节在前（Modbus 的大端约定）。</summary>
    private static void WriteUInt16(byte[] buffer, int offset, ushort value)
    {
        buffer[offset] = (byte)(value >> 8);
        buffer[offset + 1] = (byte)(value & 0xFF);
    }

    /// <summary>功能码 03/04 一次最多读 125 个寄存器，超出属于非法请求。</summary>
    private static void EnsureRegisterCount(ushort count)
    {
        if (count is < 1 or > 125)
            throw new ArgumentOutOfRangeException(nameof(count), "一次读取寄存器数量必须在 1..125");
    }

    public void Dispose()
    {
        Disconnect();
        _gate.Dispose();
    }
}
