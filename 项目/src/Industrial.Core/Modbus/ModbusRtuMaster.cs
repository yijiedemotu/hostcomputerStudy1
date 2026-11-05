using System.Diagnostics;
using Industrial.Core.Transport;

namespace Industrial.Core.Modbus;

/// <summary>
/// Modbus RTU 主站（RS485 半双工，一问一答）。
///
/// 帧 = 从站地址(1) + PDU + CRC16(2，低字节在前)。没有报文头，
/// 所以必须靠“功能码 + 字节数”推算应答长度，靠 3.5 字符静默时间分帧。
///
/// 现场最容易踩的坑都在这里处理了：
///  * 发送前清空缓冲区（否则上一帧残包会被当成本帧应答）；
///  * 两次发送之间至少留 3.5 个字符时间（9600 下约 4ms，取 10ms 更稳）；
///  * 校验从站地址、功能码、CRC，任一不符立刻报错，不把脏数据写进数据库。
/// </summary>
public sealed class ModbusRtuMaster : IModbusMaster
{
    private readonly ISerialPort _port;       // 串口抽象，测试时可以换成假串口
    private readonly int _timeoutMs;          // 等应答的总超时
    private readonly SerialSettings _settings;
    private readonly SemaphoreSlim _gate = new(1, 1); // 异步排队，保证一问一答
    private readonly object _sync = new();             // 串口读写的同步锁

    // 记录上一帧发完的时刻，用来计算 3.5 字符静默时间
    private readonly Stopwatch _sinceLastFrame = Stopwatch.StartNew();

    // 串口是不是本对象打开的：只有自己打开的才负责关闭，避免误关别人正在用的口
    private bool _ownsPort;

    public ModbusRtuMaster(ISerialPort port, SerialSettings settings, int timeoutMs = 1000)
    {
        _port = port;
        _settings = settings;
        _timeoutMs = timeoutMs;
    }

    public string Endpoint => _settings.ToString();
    public bool IsConnected => _port.IsOpen;

    /// <summary>打开串口。如果调用前已经是打开状态，就直接复用。</summary>
    public Task ConnectAsync(CancellationToken ct = default)
    {
        if (!_port.IsOpen)
        {
            _port.Open();
            _ownsPort = true;
            // 清掉上电/插拔时积累的杂数据，避免第一帧就读到脏包
            _port.DiscardBuffers();
        }
        return Task.CompletedTask;
    }

    /// <summary>关闭串口（仅当串口是本对象打开的）。</summary>
    public void Disconnect()
    {
        if (_ownsPort && _port.IsOpen) _port.Close();
        _ownsPort = false;
    }

    public Task<bool[]> ReadCoilsAsync(byte unitId, ushort startAddress, ushort count, CancellationToken ct = default)
        => ReadBitsAsync(unitId, 0x01, startAddress, count, ct);

    public Task<bool[]> ReadDiscreteInputsAsync(byte unitId, ushort startAddress, ushort count, CancellationToken ct = default)
        => ReadBitsAsync(unitId, 0x02, startAddress, count, ct);

    public Task<ushort[]> ReadHoldingRegistersAsync(byte unitId, ushort startAddress, ushort count, CancellationToken ct = default)
        => ReadRegistersAsync(unitId, 0x03, startAddress, count, ct);

    public Task<ushort[]> ReadInputRegistersAsync(byte unitId, ushort startAddress, ushort count, CancellationToken ct = default)
        => ReadRegistersAsync(unitId, 0x04, startAddress, count, ct);

    /// <summary>读寄存器（03/04 共用）。请求 PDU 5 字节：功能码 + 地址(2) + 数量(2)。</summary>
    private async Task<ushort[]> ReadRegistersAsync(byte unitId, byte function, ushort startAddress, ushort count, CancellationToken ct)
    {
        if (count is < 1 or > 125) throw new ArgumentOutOfRangeException(nameof(count), "一次读取寄存器数量必须在 1..125");

        // 地址和数量都是 16 位，拆成高字节、低字节放进 PDU
        var pdu = new byte[5];
        pdu[0] = function;
        pdu[1] = (byte)(startAddress >> 8);
        pdu[2] = (byte)(startAddress & 0xFF);
        pdu[3] = (byte)(count >> 8);
        pdu[4] = (byte)(count & 0xFF);

        var pduResponse = await ExecuteAsync(unitId, pdu, ct).ConfigureAwait(false);
        if (pduResponse[0] != function)
            throw new ModbusProtocolException($"应答功能码不符：期望 0x{function:X2}，实际 0x{pduResponse[0]:X2}");

        var byteCount = pduResponse[1];
        if (byteCount != count * 2)
            throw new ModbusProtocolException($"应答字节数不符：期望 {count * 2}，实际 {byteCount}");

        // 每两个字节拼成一个寄存器，高字节在前
        var values = new ushort[count];
        for (var i = 0; i < count; i++)
            values[i] = (ushort)((pduResponse[2 + i * 2] << 8) | pduResponse[3 + i * 2]);
        return values;
    }

    /// <summary>读线圈/离散输入（01/02 共用）。8 个开关量压在一个字节里。</summary>
    private async Task<bool[]> ReadBitsAsync(byte unitId, byte function, ushort startAddress, ushort count, CancellationToken ct)
    {
        if (count is < 1 or > 2000) throw new ArgumentOutOfRangeException(nameof(count), "位读取数量必须在 1..2000");

        var pdu = new byte[5];
        pdu[0] = function;
        pdu[1] = (byte)(startAddress >> 8);
        pdu[2] = (byte)(startAddress & 0xFF);
        pdu[3] = (byte)(count >> 8);
        pdu[4] = (byte)(count & 0xFF);

        var pduResponse = await ExecuteAsync(unitId, pdu, ct).ConfigureAwait(false);
        if (pduResponse[0] != function)
            throw new ModbusProtocolException($"应答功能码不符：期望 0x{function:X2}，实际 0x{pduResponse[0]:X2}");

        // 9 个位要 2 个字节，所以用 (count + 7) / 8 向上取整
        var expected = (count + 7) / 8;
        if (pduResponse[1] != expected)
            throw new ModbusProtocolException($"位应答字节数不符：期望 {expected}，实际 {pduResponse[1]}");

        var values = new bool[count];
        for (var i = 0; i < count; i++)
            // 取出第 i 位：字节下标 i/8，位下标 i%8
            values[i] = (pduResponse[2 + i / 8] & (1 << (i % 8))) != 0;
        return values;
    }

    /// <summary>写单个线圈（05）：开 = 0xFF00，关 = 0x0000。</summary>
    public async Task WriteSingleCoilAsync(byte unitId, ushort address, bool value, CancellationToken ct = default)
    {
        var pdu = new byte[5];
        pdu[0] = 0x05;
        pdu[1] = (byte)(address >> 8);
        pdu[2] = (byte)(address & 0xFF);
        pdu[3] = value ? (byte)0xFF : (byte)0x00;
        pdu[4] = 0x00;

        var response = await ExecuteAsync(unitId, pdu, ct).ConfigureAwait(false);
        if (response[0] != 0x05) throw new ModbusProtocolException($"写线圈应答功能码不符：0x{response[0]:X2}");
    }

    /// <summary>写单个保持寄存器（06），从站原样回显请求帧。</summary>
    public async Task WriteSingleRegisterAsync(byte unitId, ushort address, ushort value, CancellationToken ct = default)
    {
        var pdu = new byte[5];
        pdu[0] = 0x06;
        pdu[1] = (byte)(address >> 8);
        pdu[2] = (byte)(address & 0xFF);
        pdu[3] = (byte)(value >> 8);
        pdu[4] = (byte)(value & 0xFF);

        var response = await ExecuteAsync(unitId, pdu, ct).ConfigureAwait(false);
        if (response[0] != 0x06) throw new ModbusProtocolException($"写寄存器应答功能码不符：0x{response[0]:X2}");
    }

    /// <summary>连续写多个寄存器（10 / 0x10），写 32 位数据时用。</summary>
    public async Task WriteMultipleRegistersAsync(byte unitId, ushort startAddress, ushort[] values, CancellationToken ct = default)
    {
        if (values.Length is < 1 or > 123) throw new ArgumentOutOfRangeException(nameof(values), "一次最多写 123 个寄存器");

        var pdu = new byte[6 + values.Length * 2];
        pdu[0] = 0x10;
        pdu[1] = (byte)(startAddress >> 8);
        pdu[2] = (byte)(startAddress & 0xFF);
        pdu[3] = (byte)(values.Length >> 8);
        pdu[4] = (byte)(values.Length & 0xFF);
        pdu[5] = (byte)(values.Length * 2); // 数据区字节数
        for (var i = 0; i < values.Length; i++)
        {
            pdu[6 + i * 2] = (byte)(values[i] >> 8);
            pdu[7 + i * 2] = (byte)(values[i] & 0xFF);
        }

        var response = await ExecuteAsync(unitId, pdu, ct).ConfigureAwait(false);
        if (response[0] != 0x10) throw new ModbusProtocolException($"批量写应答功能码不符：0x{response[0]:X2}");
    }

    /// <summary>
    /// 串行化执行一次 RTU 事务，返回应答 PDU。
    /// 串口是阻塞 API，所以用 Task.Run 把阻塞收发挪到线程池，
    /// 这样 UI 线程 await 时不会被卡住。
    /// </summary>
    private async Task<byte[]> ExecuteAsync(byte unitId, byte[] pdu, CancellationToken ct)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return await Task.Run(() => Transact(unitId, pdu, ct), ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release(); // 保证异常时也能放行下一帧
        }
    }

    /// <summary>真正的一问一答：加站号 → 加 CRC → 清缓冲 → 发送 → 收应答。</summary>
    private byte[] Transact(byte unitId, byte[] pdu, CancellationToken ct)
    {
        if (!_port.IsOpen) throw new InvalidOperationException("串口未打开，请先调用 ConnectAsync。");

        // lock 防止别的线程同时读写同一个串口（例如一个线程在收、另一个在发）
        lock (_sync)
        {
            WaitInterFrameDelay(); // 保证距离上一帧至少 3.5 个字符时间
            ct.ThrowIfCancellationRequested();

            // RTU 帧 = 站号(1) + PDU + CRC(2)，所以总长是 pdu.Length + 3
            var adu = new byte[pdu.Length + 3];
            adu[0] = unitId;
            pdu.CopyTo(adu, 1);

            // CRC 覆盖“站号 + PDU”，不包含 CRC 自己
            var crc = Crc16.Compute(adu.AsSpan(0, pdu.Length + 1));
            adu[^2] = (byte)(crc & 0xFF); // 低字节在前
            adu[^1] = (byte)(crc >> 8);

            _port.DiscardBuffers(); // 扔掉上一帧残留，防止把旧应答当新应答
            _port.Write(adu);
            _sinceLastFrame.Restart(); // 记录发送时刻

            return ReadResponse(unitId, pdu[0], ct);
        }
    }

    /// <summary>
    /// 接收并校验一帧应答。
    /// 因为 RTU 没有报文头，只能先读 3 字节（站号 + 功能码 + 字节数/异常码），
    /// 再根据功能码推算整帧长度，最后校验 CRC。
    /// </summary>
    private byte[] ReadResponse(byte unitId, byte function, CancellationToken ct)
    {
        var clock = Stopwatch.StartNew();
        var head = new byte[3];

        // 先读 3 字节“帧头”
        if (!ReadAtLeast(head, 0, 3, clock, ct, out var got) || got < 2)
            throw new ModbusTimeoutException($"Modbus RTU 应答超时（{_timeoutMs}ms）：站号 {unitId} 功能码 0x{function:X2}。检查站号/波特率/接线 A-B 是否接反");

        // 站号必须对得上，否则说明总线上有多台同站号设备，或收到的不是给我们的帧
        if (head[0] != unitId)
            throw new ModbusProtocolException($"从站地址不符：期望 {unitId}，收到 {head[0]}（总线上有多台设备同站号？）");

        // 异常应答：站号 + (功能码|0x80) + 异常码 + CRC = 5 字节
        if ((head[1] & 0x80) != 0)
        {
            var exFrame = new byte[5];
            Array.Copy(head, exFrame, 3);
            ReadAtLeast(exFrame, 3, 2, clock, ct, out _);
            ValidateCrc(exFrame);
            throw new ModbusException(function, exFrame[2]);
        }

        // 正常应答：根据功能码算出整帧总长度
        int total;
        switch (head[1])
        {
            case 0x01:
            case 0x02:
            case 0x03:
            case 0x04:
                // 站号(1) + 功能码(1) + 字节数(1) + 数据(N) + CRC(2)
                total = 3 + head[2] + 2;
                break;
            case 0x05:
            case 0x06:
            case 0x0F:
            case 0x10:
                // 这些功能码的应答固定 8 字节
                total = 8;
                break;
            default:
                throw new ModbusProtocolException($"无法识别的应答功能码：0x{head[1]:X2}");
        }

        var frame = new byte[total];
        Array.Copy(head, frame, 3);
        if (total > 3 && !ReadAtLeast(frame, 3, total - 3, clock, ct, out _))
            throw new ModbusTimeoutException($"应答不完整（{_timeoutMs}ms）：收到 {got} 字节，期望 {total} 字节");

        ValidateCrc(frame);

        // 去掉站号与 CRC，只留 PDU（功能码 + 数据）
        var pduResponse = new byte[total - 3];
        Array.Copy(frame, 1, pduResponse, 0, pduResponse.Length);
        return pduResponse;
    }

    /// <summary>
    /// 从串口读满 count 个字节，最多等到超时。
    /// 串口一次 Read 可能只返回几个字节，所以要循环累加。
    /// </summary>
    private bool ReadAtLeast(byte[] buffer, int offset, int count, Stopwatch clock, CancellationToken ct, out int totalRead)
    {
        totalRead = 0;
        var filled = 0;
        while (filled < count)
        {
            ct.ThrowIfCancellationRequested();

            // 剩余可用时间；单次 Read 最多等 100ms，方便及时检查取消
            var remaining = _timeoutMs - (int)clock.ElapsedMilliseconds;
            if (remaining <= 0) return false;

            var n = _port.Read(buffer, offset + filled, count - filled, Math.Min(remaining, 100));
            if (n > 0)
            {
                filled += n;
                totalRead += n;
            }
        }
        return true;
    }

    /// <summary>CRC 不对就立即丢弃整帧，绝不把“可能错位”的数据交给上层。</summary>
    private static void ValidateCrc(ReadOnlySpan<byte> frame)
    {
        if (!Crc16.Check(frame))
            throw new ModbusProtocolException(
                $"CRC 校验失败：{Convert.ToHexString(frame.ToArray())}。常见原因：波特率不一致、干扰、终端电阻缺失、A/B 线接反");
    }

    /// <summary>
    /// 3.5 字符静默时间：RTU 靠“总线安静超过 3.5 个字符时间”来划分帧边界。
    /// 一个字符通常 10~11 位（起始位 + 8 数据位 + 校验位 + 停止位），
    /// 9600 波特率下约 1ms/字符，3.5 字符约 4ms，这里至少等 2ms。
    /// </summary>
    private void WaitInterFrameDelay()
    {
        var bitsPerChar = 1 + _settings.DataBits + (_settings.Parity == ParityMode.None ? 0 : 1) +
                          (_settings.StopBits == StopBitsMode.One ? 1 : 2);
        var charMs = bitsPerChar * 1000.0 / Math.Max(300, _settings.BaudRate);
        var required = Math.Max(2.0, 3.5 * charMs);
        var elapsed = _sinceLastFrame.Elapsed.TotalMilliseconds;
        if (elapsed < required)
            Thread.Sleep((int)Math.Ceiling(required - elapsed));
    }

    public void Dispose()
    {
        Disconnect();
        _gate.Dispose();
    }
}
