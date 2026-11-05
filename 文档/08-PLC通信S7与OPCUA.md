# 08 与 PLC 通信：S7、OPC UA 与厂商协议

> 本章目标：知道除了 Modbus 还有哪些协议、各自适用什么场景、怎么选、怎么快速上手。
> 面试常问：“你用过哪些 PLC？走的什么协议？为什么选它？”

---

## 1. 为什么不能只用 Modbus

Modbus 只能表达“地址 + 16 位数据”，设备内部的结构体、数组、符号名、时间戳都表达不了：

- 读一个 `DB10.DBD0`，你得自己算偏移；
- PLC 里改了变量顺序，上位机地址全错；
- 没有订阅机制，只能轮询；
- 没有类型信息和单位。

于是有了：

| 协议 | 厂商 | 特点 |
|---|---|---|
| S7 协议 | 西门子 | 直接读写 DB/M/I/Q，需知道偏移 |
| MC 协议 | 三菱 | 3E 帧，读写 D/M/W 区 |
| FINS | 欧姆龙 | 命令码 + 地址 |
| OPC UA | 通用标准 | 自描述、订阅、安全，跨厂商 |
| EtherNet/IP | 罗克韦尔 | CIP 对象模型 |
| Modbus TCP | 通用 | 最简单，网关首选 |

---

## 2. 西门子 S7

### 2.1 关键概念

- **ISO-on-TCP，端口 102**（不是 502）。
- 连接参数：IP + **机架号 Rack + 槽号 Slot**。
  - S7-1200/1500：Rack 0，Slot 1（部分配置为 0/1 或 0/2）
  - S7-300：Rack 0，Slot 2
  - S7-200 SMART：不支持原生 S7 协议，用 Modbus RTU/TCP 或 PC Access
- 地址写法：`DB1.DBX0.0`（位）、`DB1.DBW2`（字）、`DB1.DBD4`（双字）、`M0.0`、`I0.0`、`Q0.0`。

### 2.2 必须关闭“优化的块访问”

TIA Portal 新建 DB 默认勾选“优化的块访问”，此时变量没有固定偏移，S7 协议读不到。

```
DB 块右键 → 属性 → 取消勾选“优化的块访问” → 编译下载
```

然后用“偏移量”列做上位机地址。

### 2.3 常用库

| 库 | 说明 |
|---|---|
| `S7netplus` | 开源、NuGet 直接装、最常用 |
| `Snap7` | C++ 原生库 + C# 封装，性能好 |
| `HslCommunication` | 商业库（免费版有限制），支持多品牌 |

示例（S7.Net）：

```csharp
using S7.Net;

using var plc = new Plc(CpuType.S71200, "192.168.0.10", 0, 1);
await plc.OpenAsync();

var temp = (ushort)await plc.ReadAsync("DB1.DBW0");      // 读一个字
var speed = (uint)await plc.ReadAsync("DB1.DBD2");        // 读双字
await plc.WriteAsync("DB1.DBW10", (short)500);            // 写设定值
```

要点：

- 一个连接上可以连续读多个地址，但**每次 Read 都是一次请求**，能合并就合并（`ReadMultipleVars`）。
- S7-1200/1500 同时连接数有限（一般 8~16 个），别开一堆连接。
- 读取频率过高会被 PLC 拒绝，500ms 起步。

---

## 3. 三菱 MC 协议

- 走 TCP/UDP，默认端口随意（常见 5000/6000）。
- 3E 帧：副帧头 + 访问路径 + 命令 + 子命令 + 数据。
- 地址：`D100`（数据寄存器）、`M100`（内部继电器）、`W1A0`（链接寄存器）。
- 库：`HslCommunication`、`McProtocol`、自己实现 3E 帧。

```csharp
// HslCommunication 示例
var melsec = new MelsecMcNet("192.168.0.20", 6000);
melsec.ConnectServer();
var d100 = melsec.ReadInt16("D100").Content;
melsec.Write("D100", (short)1234);
```

---

## 4. 欧姆龙 FINS

- TCP/UDP，端口 9600。
- 命令码：`0101` 读、`0102` 写。
- 地址：`D100`、`CIO0.01`、`W1.00`。
- 库：`HslCommunication`、`OmronFins`。

---

## 5. OPC UA：未来的标准答案

### 5.1 它解决什么问题

- **自描述**：每个节点有类型、单位、范围、描述，不用再猜偏移。
- **订阅**：数据变化才推送，不用轮询。
- **安全**：证书、签名、加密。
- **跨平台**：Windows/Linux，Java/C#/Python 都有栈。

### 5.2 核心概念

| 概念 | 说明 |
|---|---|
| Endpoint | `opc.tcp://192.168.0.10:4840` |
| NodeId | 节点唯一标识，如 `ns=2;s=Channel1.Device1.Temperature` |
| Session | 会话，需要认证 |
| Subscription | 订阅，设置 PublishingInterval |
| MonitoredItem | 订阅的具体节点 |
| SecurityPolicy | None / Basic256Sha256 等 |

### 5.3 C# 客户端

NuGet：`OPCFoundation.NetStandard.Opc.Ua`（官方栈，包体较大）

```csharp
var endpoint = CoreClientUtils.SelectEndpoint("opc.tcp://192.168.0.10:4840", useSecurity: false);
var configuration = new ApplicationConfiguration { /* ... */ };

using var session = await Session.Create(configuration, new ConfiguredEndpoint(null, endpoint),
    false, "UpperComputer", 60000, null, null);

var node = NodeId.Parse("ns=2;s=Channel1.Device1.Temperature");
var value = await session.ReadValueAsync(node);
Console.WriteLine($"{value.Value} {value.SourceTimestamp}");

// 订阅
var subscription = new Subscription(session.DefaultSubscription) { PublishingInterval = 500 };
var item = new MonitoredItem(subscription.DefaultItem)
{
    StartNodeId = node,
    AttributeId = Attributes.Value,
    SamplingInterval = 250
};
item.Notification += (m, e) => Console.WriteLine($"新值：{m.Value}");
subscription.AddItem(item);
session.AddSubscription(subscription);
subscription.Create();
```

### 5.4 服务端/网关

- **KEPServerEX**（Kepware）：商业网关，把 S7/Modbus/三菱统一成 OPC UA，国内项目极常见。
- **UaExpert**：免费调试客户端，面试前装上点一遍。
- 西门子 S7-1500 固件 2.0+ 自带 OPC UA 服务器。

### 5.5 证书与安全

- 首次连接需要信任服务器证书（客户端证书也要被服务器信任）。
- 生产环境不要用 `SecurityPolicy.None`，至少 `Basic256Sha256` + `SignAndEncrypt`。
- 用户名密码认证优于匿名。

---

## 6. OPC DA：老系统的坑

- 基于 COM/DCOM，跨机器配置极其麻烦（防火墙、DCOM 权限、用户一致）。
- 只支持 Windows，32/64 位不兼容问题多。
- 新项目**不要选 OPC DA**，用 OPC UA 或网关转换。

---

## 7. MQTT / HTTP：上云与 MES

| 协议 | 场景 | 特点 |
|---|---|---|
| MQTT | 设备上云、远程监控 | 轻量、发布/订阅、断线重连 |
| HTTP/REST | MES、ERP、报表 | 通用、简单，实时性差 |
| WebSocket | 网页看板 | 双向、低延迟 |
| gRPC | 内部服务 | 高性能、二进制 |

MES 对接常见流程：

```
上位机 → 扫条码 → 调 MES 接口(HTTP+JSON) → 返回工单/工艺参数 → 下发 PLC
         ↑ 失败要重试、要缓存、要幂等（用唯一流水号）
```

---

## 8. 选型决策树

```
设备是西门子 1200/1500？
 ├─ 是 → 能开 OPC UA 就 OPC UA（推荐），否则 S7 协议（S7netplus）
设备是三菱/欧姆龙/台达/汇川？
 ├─ 支持 Modbus → 优先 Modbus（简单、稳定）
 ├─ 不支持 → 厂商协议（MC/FINS）或 HslCommunication
设备是仪表/变频器/温控器？
 ├─ 99% 是 Modbus RTU/TCP
设备型号很杂、品牌很多？
 ├─ 上 OPC UA 网关（Kepware），上位机只对接 OPC UA
要上云/多端？
 └─ MQTT 或 HTTP 上报，本地仍保留直连
```

---

## 9. 通信性能与稳定性

| 做法 | 效果 |
|---|---|
| 合并读取（一次读多个连续地址） | 减少帧数，提升 5~10 倍 |
| 按变化订阅（OPC UA） | 几乎零冗余流量 |
| 合理采集周期（≥200ms） | 避免 PLC 通信资源耗尽 |
| 单连接复用 | 避免连接数超限 |
| 断线重连 + 数据缓存 | 网络抖动不丢数据 |
| 通信与 UI 分离 | 界面不卡 |
| 每次请求带超时 | 不会永久挂起 |

---

## 10. 安全提醒（真实事故）

1. **不要把 502/102/4840 直接暴露在公网**，会被扫描、被写入。
2. PLC 的写保护要开，上位机只写允许的地址。
3. 数据库密码不要明文写在配置文件（用 DPAPI 或环境变量）。
4. 操作要有审计日志（谁、什么时候、写了什么值）。
5. 关键写操作要二次确认（弹窗 + 权限）。

---

## 11. 本章验收

- [ ] 能说清 S7 与 Modbus 的区别、各自端口
- [ ] 知道“优化的块访问”为什么会让上位机读不到数据
- [ ] 能解释 OPC UA 的 Endpoint/NodeId/Subscription
- [ ] 能用 UaExpert 连一个 OPC UA 服务器并读一个节点（可用公开测试服务器）
- [ ] 能根据设备类型给出协议选型建议
- [ ] 知道 S7-1200 同时连接数有限，不能乱开连接

下一章：`09 数据存储：CSV、SQLite、SQL Server.md`
