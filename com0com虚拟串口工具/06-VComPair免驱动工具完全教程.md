# 06 · VComPair 免驱动虚拟串口工具完全教程

> 这是本仓库自研的工具：**不装驱动、不要管理员、不依赖 com0com**，却能让串口调试助手完整收发。
> 本文 = 使用手册 + 原理 + 代码导读 + 排错 + 扩展任务。

---

## 1. 为什么要有这个工具

`com0com` 是内核驱动，能造出设备管理器里看得见的 `COM5`/`COM6`。但代价是：

- 需要管理员权限；
- Win10/11 上常被驱动签名拦住（Code 52），要开测试模式甚至关 Secure Boot；
- ARM64 没有官方包；
- 公司电脑 IT 策略通常禁止装第三方内核驱动。

而本仓库的串口调试助手底层已经抽象成 `ISerialPort` 接口（见
`项目/src/Industrial.Core/Transport/ISerialPort.cs`）。这意味着**只要再写一个实现，就能换一条"线"**，业务代码一行都不用改。

VComPair 就是这条用户态的线：本机 TCP 回环 `127.0.0.1:9527` 上挂两个端点 `VCOM1` / `VCOM2`，
A 写进去的字节立刻从 B 读出来。效果等同空调制解调器线，但只是一个普通 .NET 窗口程序。

| | com0com | VComPair |
|---|---|---|
| 端口名 | `COM5` / `COM6`（内核设备） | `VCOM1` / `VCOM2`（逻辑端点） |
| 谁认得 | 所有程序 | 已适配的程序（本仓库串口助手 + 工具自带终端） |
| 权限 | 管理员 | 普通用户 |
| 驱动 | 内核 `.sys` | 无 |
| 关闭后 | 端口要卸载才消失 | 关掉窗口就彻底没了 |
| 风险 | 内核态，装错影响系统 | 只监听 `127.0.0.1`，几乎零风险 |

---

## 2. 5 分钟跑通（照着点）

### 2.1 启动服务端

```powershell
cd D:\上位机\上位机学习大全
dotnet run --project com0com虚拟串口工具/VComPair
```

或者**双击** `com0com虚拟串口工具/一键启动工具.bat`（自动编译并启动）。

窗口出现后点顶部 **「① 启动服务」**。正常状态：

```
● 服务运行中 · 127.0.0.1:9527
VCOM1：空闲（可被外部程序占用）   |   VCOM2：已连接 · 内置终端
```

> 启动时工具会**自动把 VCOM2 连上**当成"设备侧"，把 **VCOM1 空出来**给外部串口助手。这是刻意设计的默认值。

### 2.2 启动串口助手

```powershell
cd D:\上位机\上位机学习大全
dotnet run --project 项目/src/SerialAssistant
```

端口下拉框选 **VCOM1**（列表末尾固定有两项 VCOM1/VCOM2），点 **「打开串口」**。
状态变成"已打开 VCOM1"，提示语写明"参数仅作显示，虚拟链路不限制波特率"。

### 2.3 互发

| 动作 | 结果 |
|---|---|
| 在 VComPair 左下 **VCOM2** 面板发送框输入 `Hello COM!` → 点「发送」 | 串口助手接收区出现 `[时间] Hello COM!` |
| 在串口助手发送 `01 03 00 00 00 0A C5 CD` → 点「发送 (Ctrl+Enter)」 | VCOM2 接收区显示这一帧，右侧监控窗口打印流向 |
| 右侧「数据监控」 | `[19:20:33.451] VCOM2→VCOM1 8B \| 01 03 00 00 00 0A C5 CD \| ....` |

**至此通讯闭环完成。** 定时发送、HEX 显示、时间戳、保存日志等功能全部照常可用。

> 界面实际效果可先看同目录的 `VComPair-界面预览.png`（服务已启动、VCOM2 发出一帧后的截图）。

### 2.4 收尾

先关串口助手（释放 VCOM1），再在 VComPair 点「停止服务」，最后关窗口。
工具会主动断开所有连接，不留后台进程。

---

## 3. 界面逐个控件说明

### 顶部工具条

| 控件 | 作用 |
|---|---|
| `○ 服务未启动` / `● 服务运行中` | 红色 = 未启动，绿色 = 运行中（固定宽度，启动后不会把整排控件挤开） |
| `端口`（默认 9527） | 本机回环端口。被占用时改成 9528 再点启动；改端口后**串口助手也要同步**（见 §6.6） |
| `① 启动服务` | 启动 `PairHub`，并自动连接 VCOM2 内置终端 |
| `停止服务` | 断开全部连接、停监听 |
| `回环模式` | 勾上后数据发回本端而不是对端，用于单端自测（鼠标悬停有说明） |
| `清空监控` | 清空右侧监控窗口 |
| `使用说明` | 弹出快速说明 |

### 左侧两个终端面板

| 控件 | 作用 |
|---|---|
| `离线` / `在线` + `连接`/`断开` | 手动占用或释放某个端点。VCOM1 默认**不连**（留给外部程序） |
| `HEX 发` | 勾上按十六进制解析发送框（`01 03 00 0A`），否则按 UTF-8 文本 |
| `HEX 显` | 接收区按 `XX XX` 显示，否则按 UTF-8 文本 |
| `自动应答` | 收到数据后，把**发送框里的内容**原样回一帧（模拟仪表/从站） |
| `清空` | 清本面板接收区 |
| 接收区 | `TX →` 是本端发出的内容，`RX ←` 是对端来的内容，都带时间戳 |
| 发送框 + `发送` | 手动发一帧，发完立刻在本端接收区回显一行 `TX →` |

> 三个短标签（`HEX 发` / `HEX 显` / `回环模式`）都有鼠标悬停提示；
> 用短文本是为了让窗口缩到最小尺寸时选项条仍能单行放下，不出现横向滚动条。

### 右侧

| 控件 | 作用 |
|---|---|
| 数据监控 | 打印每帧的 `时间 / 来源→目标 / 字节数 / HEX / ASCII`，以及连接、断开、缓存补发等事件；长行自动换行 |
| 操作步骤 | 常驻的六步提示 |
| 底部状态栏 | 实时显示 VCOM1 / VCOM2 被谁占用 |

---

## 4. 架构与协议

```
┌───────────────────────────── VComPair.exe ─────────────────────────────┐
│                                                                        │
│   TerminalPanel(VCOM1) ──┐                 ┌── TerminalPanel(VCOM2)      │
│        VirtualClient     │                 │        VirtualClient        │
│             │            │                 │             │              │
│             └────────────┼───── TCP ───────┼─────────────┘              │
│                          ▼                 ▼                            │
│                  ┌─────────────────────────────────┐                   │
│                  │  PairHub  127.0.0.1:9527        │                   │
│                  │  VCOM1 写 ──► VCOM2 读           │                   │
│                  │  VCOM2 写 ──► VCOM1 读           │                   │
│                  │  离线缓存 / 独占 / 回环 / 监控     │                   │
│                  └─────────────────────────────────┘                   │
└──────────────────────────────┬─────────────────────────────────────────┘
                               │ TCP 回环（同一台机器）
┌──────────────────────────────▼─────────────────────────────────────────┐
│  项目/src/SerialAssistant  →  Industrial.Core/Transport/VirtualSerialPort│
│  （实现 ISerialPort：Open/Close/Read/Write/DiscardBuffers）              │
└────────────────────────────────────────────────────────────────────────┘
```

### 握手协议

连接建立后，客户端先发**一行 UTF-8 文本**（`\n` 结束）：

```
HELLO|VCOM1|SerialAssistant\n        ← 客户端（端口名 + 来源标签）
HELLO|VCOM2|内置终端\n
```

服务端回一行：

```
+OK|VCOM1\n                          ← 接受
-ERR|VCOM1 已被占用（同一串口只能被一个程序打开，请先断开旧连接）\n   ← 拒绝
```

**握手之后的字节全部当作串口裸数据透明转发**，不分包、不转义、不加帧头——和真实串口的行为一致。
中文错误提示和"内置终端"标签用 UTF-8 传输，不会变 `?`。

### 关键行为（都和真实串口对齐）

| 行为 | 实现 |
|---|---|
| 一个端口只能被一个程序打开 | `PairHub._sessions` 按端点存唯一会话，第二个连接返回 `-ERR` |
| 对端不在线时数据不丢 | 每个端点有最多 128 帧的缓存队列，上线后 `FlushPending` 补发 |
| 读超时返回 0 | `VirtualSerialPort.Read` 用 `DataAvailable` 轮询，语义与 `SerialPortNative.Read` 完全一致 |
| 关闭要干净 | 先停定时器 → 关端口 → 等接收线程退出 → 恢复界面（串口助手原有逻辑） |
| 波特率等参数 | 虚拟线没有电气层，参数仅作显示；两端填一样是好习惯 |

---

## 5. 与串口助手的集成（只改了两处）

### 5.1 新增一个 `ISerialPort` 实现

`项目/src/Industrial.Core/Transport/VirtualSerialPort.cs`：

```csharp
public sealed class VirtualSerialPort : ISerialPort
{
    public static readonly string[] VirtualPortNames = ["VCOM1", "VCOM2"];
    public void Open()      { /* 连 127.0.0.1:9527 + HELLO 握手 */ }
    public void Close()     { /* 释放 socket */ }
    public void Write(byte[] data) { /* socket 写 */ }
    public int Read(byte[] buffer, int offset, int count, int timeoutMs)
                            { /* DataAvailable 轮询，超时返回 0 */ }
    public void DiscardBuffers() { /* 排空可读字节 */ }
}
```

### 5.2 串口助手按端口名分流

`项目/src/SerialAssistant/MainForm.cs`：

```csharp
// 字段从具体类改成接口
private ISerialPort? _port;

// 端口列表：物理口 + 固定追加两个虚拟口
var physical = SerialPortNative.GetPortNames();
var ports = physical.Concat(VirtualSerialPort.VirtualPortNames).ToArray();

// 打开时自动选实现
ISerialPort port = VirtualSerialPort.IsVirtualPort(settings.PortName)
    ? new VirtualSerialPort(settings.PortName)
    : new SerialPortNative(settings);
```

**业务代码（后台接收循环、HEX 解析、定时发送、日志保存）一行没动。**
这就是面向接口编程的价值，也是面试里"你怎么做到不依赖硬件测试"的标准答案。

### 5.3 源码文件清单

| 文件 | 职责 |
|---|---|
| `VComPair/Program.cs` | WinForms 入口（`ApplicationConfiguration.Initialize`） |
| `VComPair/MainForm.cs` | 主窗口：工具条、两个终端、监控、状态栏；启停服务。布局用 **Dock + SplitContainer** |
| `VComPair/TerminalPanel.cs` | 单个端点面板：连接/断开、HEX/文本收发、自动应答、TX/RX 回显；内部用 Dock（Top/Bottom/Right/Fill） |
| `VComPair/PairHub.cs` | 服务端：监听、握手、独占、转发、离线缓存、回环、事件 |
| `VComPair/VirtualClient.cs` | 工具内置终端的客户端（和 VirtualSerialPort 同协议） |
| `VComPair/ProtocolHelper.cs` | HEX 解析、HEX/ASCII 显示 |
| `项目/src/Industrial.Core/Transport/VirtualSerialPort.cs` | 串口助手侧的客户端实现 |
| `项目/src/Industrial.Core/Transport/ISerialPort.cs` | 两种实现共用的抽象 |

全部零 NuGet 依赖，`net8.0-windows`，用 `dotnet build` 直接编译。

> **布局实现说明（踩过的坑）**：最初用嵌套 `TableLayoutPanel`，单列容器没设
> `ColumnStyles(Percent 100)`，列宽按 AutoSize 塌缩，控件全部挤在左上角。
> 现在改成 Dock + SplitContainer：Fill 控件第一个 Add、边缘控件后 Add，
> 终端面板放进 `SplitterPanel` 必须显式 `Dock = DockStyle.Fill`，
> 且不要**在构造函数里**设置 `Panel1MinSize/Panel2MinSize`（此时控件没尺寸，
> SplitContainer 会校验 `SplitterDistance` 并抛异常），分栏位置改在 `Shown` 事件里设置。

---

## 6. 进阶用法

### 6.1 自动应答：把 VCOM2 变成"会回包的仪表"

1. 在 VCOM2 面板发送框填应答内容，例如 `01 03 02 00 64 B8 5B`；
2. 勾 `HEX 发送` + `自动应答`；
3. 串口助手（VCOM1）发任意一帧 → VCOM2 收到后自动回这一帧。

这样就能练"请求-应答"型协议，甚至拼一个简化版 Modbus RTU 从站。
**注意**：两端同时开自动应答 + 回环模式会无限互发，别这么配。

### 6.2 两个内置终端互测（不启动外部助手）

把 VCOM1 面板也点「连接」，两个面板直接互发。适合演示、录屏、快速验证工具本身。
记得此时外部串口助手**不能**占着 VCOM1。

### 6.3 回环模式

勾「回环模式」后，VCOM1 发的数据回到 VCOM1 自己。用于单实例自测"发送→接收"链路，
不需要对端。这是调试助手"自发自收"的经典手法。

### 6.4 离线缓存

对端没连时发送，数据不会丢：服务端会打印"VCOM2 未连接，已缓存 1 帧"，
等对端连上再补发。真实串口线在另一端没打开时也会有类似缓冲行为（com0com 默认如此）。

### 6.5 端口独占提示

如果 VCOM1 已被串口助手占用，工具里再点 VCOM1 的「连接」会收到中文错误：
`VCOM1 已被占用（同一串口只能被一个程序打开，请先断开旧连接）`。
和真实串口报"端口被占用"完全一致——这是刻意保留的教学点。

### 6.6 换监听端口

9527 被别的程序占了：把窗口里"监听端口"改成 `9528` 再启动。
**同时**要给串口助手传新端口。最简单的方式是改 `VirtualSerialPort` 的默认值：

```csharp
// 项目/src/Industrial.Core/Transport/VirtualSerialPort.cs
public const int DefaultPort = 9528;   // 与工具窗口保持一致
```

（或者在 `new VirtualSerialPort(name, port: 9528)` 处传入，见 `SerialAssistant/MainForm.cs`。）

---

## 7. 常见问题

| 现象 | 原因 / 处理 |
|---|---|
| 串口助手打开 VCOM1 报"连不上虚拟串口服务" | 工具没启动，或没点「① 启动服务」。看窗口是否显示 `● 服务运行中` |
| "连接虚拟串口服务超时" | 端口不一致（工具改了 9528、助手还连 9527），见 §6.6 |
| 启动时弹"启动失败：通常每个套接字地址只允许使用一次" | 9527 被占用，换个端口 |
| "VCOM1 已被占用" | 一个端点只能一个程序。先关旧窗口 / 在工具里点「断开」 |
| 端口下拉框看不到 VCOM1 | 用的是旧版本代码；确认 `MainForm.RefreshPortList()` 已追加 `VirtualPortNames` |
| 发得出去收不到 | 先检查右侧监控有没有 `→` 流向；再看接收端是否勾了"暂停显示"、是否同一端点回环 |
| 中文变问号 | 握手已用 UTF-8；若仍乱码，检查串口助手"编码"下拉是否选了 UTF-8（国产仪表才切 GB2312） |
| 关闭窗口后端口还在 | 不会出现：VComPair 是用户态服务，关窗口即消失。这是它比 com0com 干净的地方 |
| 第三方软件选不了 VCOM1 | **设计如此**。VCOM 不是设备管理器里的 COM 口，见 [02/03 号教程](README.md) 装 com0com |
| 想让两个不同电脑通讯 | 当前只监听 `127.0.0.1`。跨机需要把 `PairHub` 改成监听 `IPAddress.Any` 并加认证（见扩展任务） |
| 公司电脑不让装驱动 | 正是本工具的目标场景，普通用户权限即可运行 |

---

## 8. 已实测通过的自检项

本仓库交付前对这条链路做过端到端实测（协议层 + 真实 GUI），全部通过：

1. `VCOM1 → VCOM2` 发送 8 字节 Modbus RTU 帧 `01 03 00 00 00 0A C5 CD`，对端完整收到；
2. `VCOM2 → VCOM1` 发送文本 `PONG 123\r\n`，对端完整收到；
3. 无数据时 `Read(..., 60ms)` **返回 0 而不是抛异常**（与真实串口语义一致）；
4. 同一端点被第二个程序打开时**正确拒绝**，并返回中文原因；
5. 回环模式 `AA BB` 原样返回本端；
6. 对端离线时发送的数据**缓存并在上线后补发**；
7. 工具内置终端（`VirtualClient`）双向互通，中文标签"内置终端A/B"不乱码；
8. GUI 工具启动后保持运行、无异常退出；`dotnet build` 0 错误 0 警告；
9. **真实窗口交互自检**：启动窗体 → 点「① 启动服务」→ VCOM2 自动连上 → 发送 `PING` → 监控窗口打印 `VCOM2→VCOM1 4B | 50 49 4E 47 | PING`，本端回显 `TX → PING`；
10. **布局自检**：默认窗口 1098×664 下两个终端各占约 636×295、接收区 624×156、监控区 456×394；缩到最小 920×560 时主工具条与终端选项条**均无溢出**、无控件重叠。

> 界面实际效果见同目录 `VComPair-界面预览.png`。

---

## 9. 局限（必须说清楚）

- `VCOM1/VCOM2` **不是** Windows 设备对象，不会出现在设备管理器，第三方只认 `COMx` 的软件用不了；
- 不模拟波特率、校验位、停止位、DTR/RTS 握手、Modbus RTU 的 3.5 字符间隔等电气/时序特性；
- 只监听 `127.0.0.1`，不跨机器；
- 不做身份认证，任何本机程序都能连上（本机回环场景可接受）；
- 它是**学习/联调工具**，不是生产设备通讯组件。

需要以上任一项 → 用 com0com（[02](02-com0com下载安装完整教程.md) / [03](03-创建串口对与命令行详解.md)）或真实硬件。

---

## 10. 扩展任务（练手）

- [ ] 给握手加一个简单口令，拒绝未授权的本机连接
- [ ] 把 `PairHub` 改成监听 `IPAddress.Any`，支持两台电脑通过虚拟串口对联（注意加认证）
- [ ] 增加"数据噪声/丢包/延迟"开关，用来测试上位机的容错与重连
- [ ] 在监控窗口加"按 HEX 过滤"和"导出 pcap-like 文本"
- [ ] 内置一个真正的 Modbus RTU 从站（按功能码 03/06 自动应答），让项目 2 也能走 RTU 链路
- [ ] 把 `VirtualSerialPort` 抽成可配置注入（`host/port` 从 appsettings.json 读）
- [ ] 增加第三个端点 + 一对多分发，模拟 `hub4com`

---

## 11. 相关文档

- [README 导航](README.md) · [01 原理](01-com0com是什么与工作原理.md) · [02 安装](02-com0com下载安装完整教程.md) · [03 建端口对](03-创建串口对与命令行详解.md) · [04 联调](04-串口调试助手联调实战.md) · [05 排错](05-故障排查与卸载.md)
- [文档/17-项目一串口调试助手完全教程.md](../文档/17-项目一串口调试助手完全教程.md)
- [文档/06-通信基础串口TCP与协议.md](../文档/06-通信基础串口TCP与协议.md)
