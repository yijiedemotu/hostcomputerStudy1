using System.Net.Sockets;
using Answers.ConsoleApp.Modbus;

namespace Answers.ConsoleApp;

/// <summary>
/// 第 07 章 · 练习 1：手动发一帧 Modbus RTU 请求，并验证链路。
///
/// 题目：在串口助手里发 01 03 00 00 00 0A C5 CD，观察从站应答。
///
/// 帧的含义（从左到右）：
///   01      从站地址（unit id），表示找 1 号设备
///   03      功能码：读保持寄存器（Holding Register）
///   00 00   起始地址 0
///   00 0A   读取 10 个寄存器
///   C5 CD   CRC16 校验，低字节 C5 在前、高字节 CD 在后
///
/// 本练习做两件事：
/// 1. 检查这帧自带的 CRC 是否正确；
/// 2. 如果本机开着 Python 模拟器，就改用等价的 Modbus TCP 帧实际读一次。
///    TCP 帧没有 CRC，只多 7 个字节的 MBAP 头，其余功能码和地址格式相同。
/// </summary>
internal static class Exercise01_SendFrame
{
    public static async Task RunAsync()
    {
        ConsoleUi.Section("练习 1：串口助手手发 01 03 00 00 00 0A C5 CD，并用模拟器验证");

        // 把文本帧解析成字节数组，后面的 CRC 校验和发送都基于字节。
        var frame = HexUtil.ParseHex("01 03 00 00 00 0A C5 CD");
        ConsoleUi.KeyValue("RTU 请求帧", HexUtil.ToHex(frame));

        // Crc16.Check 会重新计算前 6 个字节的 CRC，再和帧尾两个字节比较。
        ConsoleUi.KeyValue("帧内 CRC 是否正确", Crc16.Check(frame) ? "正确（低字节 C5 在前）" : "错误");
        ConsoleUi.Answer("串口助手设置 9600-8-N-1，勾选 HEX 发送，发送上面 8 个字节；" +
                         "正常应答以 01 03 14 开头（10 个寄存器 = 20 字节数据），最后 2 字节是 CRC。");

        // 允许用环境变量改端口：模拟器占用 502 时，可以起在 1502。
        var port = int.TryParse(Environment.GetEnvironmentVariable("MODBUS_PORT"), out var p) ? p : 502;
        ConsoleUi.Note($"没有串口时，用等价的 Modbus TCP 帧连本仓库模拟器（127.0.0.1:{port}）。");
        try
        {
            // 800ms 超时：现场网络异常时不能无限等，否则采集线程会被卡死。
            await SendRawTcpAsync("127.0.0.1", port, TimeSpan.FromMilliseconds(800));
            ConsoleUi.Ok("模拟器应答验证通过：物理链路和请求格式都没问题。");
        }
        catch (Exception ex)
        {
            // 模拟器没启动不算练习失败，打印提示后继续。
            ConsoleUi.Warn($"未连接到模拟器（{ex.GetType().Name}: {ex.Message}）。");
            ConsoleUi.Note("先在另一个终端运行：python tools/modbus_slave_sim.py --verbose");
            ConsoleUi.Note("如果 502 被占用：python tools/modbus_slave_sim.py --port 1502，再设置环境变量 MODBUS_PORT=1502。");
            ConsoleUi.Note("TCP 帧没有 CRC，和 RTU 的差别只是多了 7 字节 MBAP 头。");
        }
    }

    /// <summary>
    /// 直接使用 TCP 套接字发一帧 Modbus TCP 请求，然后读应答。
    /// 这里故意不用封装好的 ModbusTcpMaster，目的是让你看清“线上真正的字节”。
    /// </summary>
    private static async Task SendRawTcpAsync(string host, int port, TimeSpan timeout)
    {
        // CancellationTokenSource 可以理解成一个“定时取消开关”：
        // 到时间后，所有带这个 Token 的异步操作都会抛 OperationCanceledException。
        using var cts = new CancellationTokenSource(timeout);

        // TcpClient 是 TCP 客户端。NoDelay = true 表示小包立即发送、不攒批，
        // 工控报文通常只有十几字节，攒批会额外增加延迟。
        using var client = new TcpClient { NoDelay = true };
        await client.ConnectAsync(host, port, cts.Token);

        // NetworkStream 是这条 TCP 连接上的读写通道。
        await using var stream = client.GetStream();

        // Modbus TCP 请求 = MBAP 头（7 字节）+ PDU。
        // 00 01            事务号，主站自己编号，应答会原样带回
        // 00 00            协议号，Modbus 固定为 0
        // 00 06            后面的字节数（单元号 1 + 功能码 1 + 地址 2 + 数量 2）
        // 01               单元号（相当于 RTU 的从站地址）
        // 03               功能码：读保持寄存器
        // 00 00 00 0A      起始地址 0，读 10 个
        var request = HexUtil.ParseHex("00 01 00 00 00 06 01 03 00 00 00 0A");
        ConsoleUi.KeyValue("等价 TCP 请求", HexUtil.ToHex(request));
        await stream.WriteAsync(request, cts.Token);
        await stream.FlushAsync(cts.Token);

        // 先读固定的 7 字节 MBAP 头。
        var header = await ReadExactAsync(stream, 7, cts.Token);

        // MBAP 第 5、6 字节是“后续长度”，大端序：高字节在前。
        // (header[4] << 8) | header[5] 把两个字节拼成一个整数。
        var length = (header[4] << 8) | header[5];

        // 长度里包含了单元号（第 7 字节已经读过），所以剩余正文长度要减 1。
        var body = await ReadExactAsync(stream, length - 1, cts.Token);
        var response = header.Concat(body).ToArray();

        ConsoleUi.KeyValue("模拟器原始应答", HexUtil.ToHex(response));

        // body[0] 是功能码，body[1] 是字节数，例如 0x14 = 20 字节。
        ConsoleUi.KeyValue("单元号 / 功能码 / 字节数", $"{header[6]} / 0x{body[0]:X2} / {body[1]}");

        // 每个寄存器 2 字节，按大端（高字节在前）拼成 ushort。
        var values = new ushort[(body.Length - 2) / 2];
        for (var i = 0; i < values.Length; i++)
            values[i] = (ushort)((body[2 + i * 2] << 8) | body[3 + i * 2]);
        ConsoleUi.KeyValue("10 个原始寄存器", string.Join(", ", values));
        ConsoleUi.Note("模拟器寄存器 0 是模温（0.1℃），1 是注射压力（0.01MPa），数值会随时间变化。");
    }

    /// <summary>
    /// 从网络流里精确读取 count 个字节。
    ///
    /// 为什么不能只 Read 一次：
    /// TCP 是“字节流”，没有消息边界。一次 Read 可能只收到半帧，
    /// 所以必须循环读，直到凑够需要的长度为止。
    /// </summary>
    private static async Task<byte[]> ReadExactAsync(NetworkStream stream, int count, CancellationToken ct)
    {
        var buffer = new byte[count];
        var offset = 0;
        while (offset < count)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset, count - offset), ct);
            if (read == 0) throw new IOException("对端关闭了连接");
            offset += read;
        }
        return buffer;
    }
}
