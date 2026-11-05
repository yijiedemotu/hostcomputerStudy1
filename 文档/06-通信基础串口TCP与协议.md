# 06 通信基础：串口、TCP、字节与协议

> 本章目标：搞懂上位机和设备之间“线怎么接、字节怎么发、帧怎么分、错了怎么查”。
> 这是上位机工程师的核心竞争力，比界面重要得多。

---

## 1. 三层视角

```
物理层：RS232 / RS485 / RS422 / 网口 / 光纤
   ↓
链路层：字节流怎么切成一帧（帧头、长度、校验、超时）
   ↓
应用层：这一帧什么意思（Modbus / S7 / 自定义协议）
```

调不通时，**永远从上往下查**：先确认线通不通，再看字节对不对，最后看解析。

---

## 2. 串口电气标准

| 标准 | 电平 | 距离 | 速率 | 拓扑 | 现场常见 |
|---|---|---|---|---|---|
| RS232 | ±3~15V 单端 | 15 m | ≤115.2k | 点对点 | 老设备、调试口、扫码枪 |
| RS485 | 差分 ±1.5~5V | 1200 m | ≤10 Mbps（短距） | 总线，一主多从 | 仪表、变频器、PLC |
| RS422 | 差分，收发分开 | 1200 m | 高 | 全双工点对多 | 少量老设备 |

RS485 现场要点：

1. **A/B 不能接反**（反了收不到数据，但不会烧）。
2. **终端电阻 120Ω**：总线两端各一个，短距离（<10m）可不加。
3. **手拉手菊花链**，不要星型分叉。
4. **屏蔽层单端接地**，不要两端接地形成地环流。
5. 变频器、伺服旁边走线要远离动力线，必要时用双绞屏蔽线 + 磁环。

DB9（RS232）常用引脚：

| 引脚 | 信号 | 说明 |
|---|---|---|
| 2 | RXD | 接收 |
| 3 | TXD | 发送 |
| 5 | GND | 地 |
| 7 | RTS | 请求发送 |
| 8 | CTS | 清除发送 |

> 交叉线：A 的 TXD(3) 接 B 的 RXD(2)，A 的 RXD(2) 接 B 的 TXD(3)，GND 对接。

---

## 3. 串口参数

`9600, 8, N, 1` 的含义：

| 参数 | 值 | 说明 |
|---|---|---|
| 波特率 | 9600 | 每秒 9600 位，双方必须一致 |
| 数据位 | 8 | 一个字节 |
| 校验 | None/Odd/Even | 老仪表常用 Even |
| 停止位 | 1/1.5/2 | 一般是 1 |
| 流控 | None | 现场几乎不用硬件流控 |

**参数不一致的典型现象**：能收到乱码或完全收不到；偶发 CRC 错。

---

## 4. C# 串口编程

官方写法（需要 `System.IO.Ports` 包）：

```csharp
using System.IO.Ports;

var port = new SerialPort("COM3", 9600, Parity.None, 8, StopBits.One)
{
    ReadTimeout = 500,
    WriteTimeout = 500,
    DtrEnable = true,
    RtsEnable = true
};

port.Open();
port.DataReceived += (s, e) =>
{
    var n = port.BytesToRead;
    var buffer = new byte[n];
    port.Read(buffer, 0, n);
    // 注意：回调在串口内部线程，不能直接操作界面
};

port.Write(new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x0A }, 0, 8);
port.Close();
```

两个流派：

| 方式 | 优点 | 缺点 | 适用 |
|---|---|---|---|
| `DataReceived` 事件 | 响应快 | 回调线程不可控、容易粘包 | 简单调试工具 |
| 后台线程 + 超时读 | 逻辑清晰、易做超时 | 多一个线程 | **工业采集推荐** |

本仓库采用第二种（`ISerialPort.Read(buffer, offset, count, timeoutMs)`），
`SerialAssistant` 的接收循环：

```csharp
while (!_closing)
{
    var n = port.Read(buffer, 0, buffer.Length, 50);
    if (n > 0) BeginInvoke(new Action<byte[]>(AppendReceived), chunk);
}
```

> 本机离线环境拿不到 `System.IO.Ports` 包，所以 `Industrial.Core/Transport/SerialPortNative.cs`
> 直接调 Win32（CreateFile/SetCommState/ReadFile）。**面试可以拿它证明你懂串口底层**。

---

## 5. 帧：怎么把字节流切开

设备不会告诉你“这一帧到哪结束”，所以协议必须自己定义边界。常见四种：

| 方式 | 例子 | 优点 | 缺点 |
|---|---|---|---|
| 固定长度 | 每帧 8 字节 | 简单 | 不灵活 |
| 帧头 + 长度 | `AA 55 LEN DATA CRC` | 通用 | 需要找帧头 |
| 分隔符 | `...\r\n` | 文本协议简单 | 数据里不能出现分隔符 |
| 静默间隔 | Modbus RTU，3.5 字符时间 | 无额外开销 | 对时序敏感 |

### 5.1 粘包与半包

TCP 是字节流，一次 `Read` 可能读到半帧，也可能读到两帧半：

```
设备发送： [帧1][帧2][帧3]
你收到：   [帧1前3字节] 然后 [帧1剩余+帧2+帧3前2字节] ……
```

**解决：缓冲区 + 循环解析。**

```csharp
var buffer = new List<byte>();

void OnData(byte[] chunk)
{
    buffer.AddRange(chunk);
    while (TryParseFrame(buffer, out var frame, out var consumed))
    {
        Handle(frame);
        buffer.RemoveRange(0, consumed);
    }
}
```

### 5.2 一个状态机解析器（自定义协议）

协议：`AA 55 | 命令 1B | 长度 1B | 数据 N | 校验 1B`

```csharp
private enum ParseState { Head1, Head2, Command, Length, Data, Checksum }

public bool TryParseFrame(List<byte> buffer, out byte[] frame, out int consumed)
{
    frame = Array.Empty<byte>();
    consumed = 0;
    var state = ParseState.Head1;
    var dataLength = 0;
    var data = new List<byte>();

    for (var i = 0; i < buffer.Count; i++)
    {
        var b = buffer[i];
        switch (state)
        {
            case ParseState.Head1:
                if (b == 0xAA) state = ParseState.Head2;
                break;
            case ParseState.Head2:
                state = b == 0x55 ? ParseState.Command : ParseState.Head1;
                break;
            case ParseState.Command:
                state = ParseState.Length;
                break;
            case ParseState.Length:
                dataLength = b;
                state = ParseState.Data;
                break;
            case ParseState.Data:
                data.Add(b);
                if (data.Count == dataLength) state = ParseState.Checksum;
                break;
            case ParseState.Checksum:
                // 校验：命令 + 长度 + 数据 的异或
                var sum = (byte)(dataLength ^ data.Aggregate(0, (a, x) => a ^ x));
                if (sum == b)
                {
                    consumed = i + 1;
                    frame = buffer.Take(consumed).ToArray();
                    return true;
                }
                // 校验失败：丢弃第一个字节，重新找帧头
                consumed = 1;
                return false;
        }
    }
    return false;
}
```

> 关键点：**永远不要假设“一次 Read 就是一帧”**。这是新手最常犯的错。

---

## 6. 校验方式

| 方式 | 算法 | 强度 | 常见协议 |
|---|---|---|---|
| 累加和 | 所有字节相加取低 8 位 | 弱 | 仪表、称重 |
| 异或 | 所有字节 XOR | 弱 | 简单自定义协议 |
| CRC16 | 多项式 0xA001 | 强 | Modbus RTU |
| LRC | 累加取反 +1 | 中 | Modbus ASCII |
| CRC32 | 以太网多项式 | 强 | 文件、固件升级 |

CRC16 手写（本仓库 `Crc16.cs`）：

```csharp
public static ushort Compute(ReadOnlySpan<byte> data)
{
    ushort crc = 0xFFFF;
    foreach (var b in data)
    {
        crc ^= b;
        for (var i = 0; i < 8; i++)
            crc = (crc & 1) != 0 ? (ushort)((crc >> 1) ^ 0xA001) : (ushort)(crc >> 1);
    }
    return crc;
}
```

已验证：`01 03 00 00 00 0A` → CRC = `0xCDC5`，帧尾低字节在前：`C5 CD`。

---

## 7. 字节序：现场最坑的一课

一个 32 位整数 `0x12345678` 放进两个 16 位寄存器，有 4 种摆法：

| 字序 | 寄存器1 | 寄存器2 | 常见设备 |
|---|---|---|---|
| ABCD | 0x1234 | 0x5678 | 西门子、施耐德、标准 Modbus |
| CDAB | 0x5678 | 0x1234 | 部分电表、台达 |
| BADC | 0x3412 | 0x7856 | 部分欧姆龙/三菱 |
| DCBA | 0x7856 | 0x3412 | 部分汇川伺服、网关 |

“数值离谱但量级差不多” → 十有八九是字序错了。
本仓库 `RegisterCodec` 四种全支持，并有自检用例。

**大小端**：Modbus 规定寄存器内部大端（高字节在前）。TCP/IP 也是大端。

---

## 8. TCP 通信

### 8.1 客户端最小实现

```csharp
using var client = new TcpClient { NoDelay = true };
await client.ConnectAsync("192.168.1.10", 502, cts.Token);
var stream = client.GetStream();

await stream.WriteAsync(frame, cts.Token);

var header = new byte[7];
await ReadExactAsync(stream, header, cts.Token);
```

`ReadExactAsync` 必须循环读，因为一次 `ReadAsync` 不保证读满：

```csharp
private static async Task ReadExactAsync(NetworkStream stream, byte[] buffer, CancellationToken ct)
{
    var offset = 0;
    while (offset < buffer.Length)
    {
        var n = await stream.ReadAsync(buffer.AsMemory(offset), ct);
        if (n == 0) throw new IOException("连接被关闭");
        offset += n;
    }
}
```

### 8.2 超时

`TcpClient.ReceiveTimeout` 对异步 API **不生效**。正确做法：

```csharp
using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
cts.CancelAfter(3000);
await stream.ReadAsync(buffer, cts.Token);   // 超时抛 OperationCanceledException
```

### 8.3 断线重连

```csharp
while (!ct.IsCancellationRequested)
{
    try
    {
        await ConnectAndPollAsync(ct);
    }
    catch (Exception ex) when (ex is IOException or SocketException or ModbusTimeoutException)
    {
        Log.Warn($"连接断开，2 秒后重连：{ex.Message}");
        await Task.Delay(2000, ct);
    }
}
```

要点：

- 重连要有退避（1s → 2s → 5s），不要死循环狂连；
- 重连成功后要**重新初始化**（有的 PLC 要求先写使能位）；
- 界面上要显示“连接中/已连接/断开”，别让操作工猜。

### 8.4 心跳

长连接要定期发心跳（读一个寄存器即可），否则中间交换机/NAT 会断开空闲连接。

---

## 9. 调试工具箱

| 工具 | 用途 | 备注 |
|---|---|---|
| 串口调试助手 | 发 HEX、看回包 | 本仓库项目 1 |
| TCP/UDP 调试助手 | 模拟服务端/客户端 | 网络调试助手、NetAssist |
| Wireshark | 抓包看原始字节 | 过滤 `tcp.port == 502` |
| Modbus Poll / Slave | 专业 Modbus 调试 | 有试用期 |
| 虚拟串口 | 无硬件时成对虚拟 COM | com0com |
| `ping` / `Test-NetConnection` | 网络连通性 | `Test-NetConnection 192.168.1.10 -Port 502` |
| 万用表 | 量 485 差分电压 | A-B 约 2~5V |

---

## 10. 现场通信故障速查

| 现象 | 优先怀疑 | 检查 |
|---|---|---|
| 完全没数据 | 线、电源、端口 | 换线、量电压、换 USB 口 |
| 有数据但乱码 | 波特率/校验/数据位 | 逐个参数对齐 |
| 偶发 CRC 错 | 干扰、终端电阻、接地 | 加 120Ω、单端接地、换屏蔽线 |
| 读回全 0 | 地址错、从站没运行 | 核对地址表、看从站指示灯 |
| 返回异常码 02 | 地址越界/基址差 1 | 40001 ↔ 0 的换算 |
| 返回异常码 0B | 网关找不到从站 | 站号、网关配置 |
| 时通时不通 | 站号冲突、总线负载 | 一台台断开排查 |
| TCP 连不上 | IP/端口/防火墙 | `Test-NetConnection`、关防火墙测试 |
| 连接几秒就断 | 心跳缺失、NAT 超时 | 加心跳、改长连接参数 |

---

## 11. 本章验收

- [ ] 能画 RS232/RS485 接线图，说出终端电阻作用
- [ ] 能解释粘包/半包，并写出缓冲区解析
- [ ] 能手算 CRC16（至少会用代码验证）
- [ ] 能说出 ABCD/CDAB/BADC/DCBA 的区别
- [ ] 能写带超时和重连的 TCP 客户端
- [ ] 用 Wireshark 抓一次 Modbus TCP 并看懂 MBAP 头

下一章：`07 Modbus 完全指南.md`
