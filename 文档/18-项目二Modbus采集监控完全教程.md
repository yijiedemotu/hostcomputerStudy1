# 18 · 项目二：Modbus 采集监控上位机完全教程

> 本仓库的**核心项目**：配置驱动的多设备采集、实时表格、趋势曲线、报警、CSV 历史、写寄存器。
> 配套代码：`项目/src/ModbusMonitor/` + `项目/src/Industrial.Core/`。
> 总览见 [14-项目实战](14-项目实战三个项目讲解.md)，协议基础见 [07-Modbus完全指南](07-Modbus完全指南.md)。

---

## 0. 这篇教什么

| 模块 | 学到什么 |
|---|---|
| 跑通与操作 | 模拟器 → 启动采集 → 表格/曲线/报警/CSV/写值 全流程 |
| 配置驱动 | `appsettings.json` 每个字段的含义，加一个点位不用改代码 |
| 采集引擎 | 地址合并批量读、后台线程、取消、故障降级为 Bad |
| 界面 | `BindingList` 数据绑定、跨线程 `BeginInvoke`、GDI+ 双缓冲自绘曲线 |
| 存储 | `ConcurrentQueue` + 定时批量落盘，采集线程不被磁盘 IO 拖慢 |
| 面试表达 | 能把"20 个点从 20 帧降到 2 帧""断线不显示过期数据"讲清楚 |

前置：`文档/07`（Modbus）、`文档/10`（多线程）、`文档/09`（数据存储）。

---

## 1. 5 分钟跑通

```powershell
cd D:\上位机\上位机学习大全\项目

# 终端 1：Modbus TCP 从站模拟器（保持运行，默认 0.0.0.0:502）
python tools\modbus_slave_sim.py

# 终端 2：上位机
dotnet run --project src\ModbusMonitor
```

窗口打开后点工具栏 **「▶ 启动采集」**。

预期：

- 表格里"炉膛温度 / 蒸汽压力 / 给水流量 / 温度设定值 / 运行状态"数值开始跳动，质量列显示 Good；
- 点某一行 → 右侧趋势曲线切换并开始绘制；
- 工具栏 `● 采集运行中`、`设备：1 台采集中`；
- 状态栏"历史 N 条"持续增长。

> 502 端口被占用：`python tools\modbus_slave_sim.py --port 1502`，
> 然后把 `src/ModbusMonitor/config/appsettings.json` 里的 `Port` 改成 1502 并重新运行。

### 顺手跑一下协议自检

```powershell
# 终端 3
dotnet run --project tools\SelfCheck -- 502
```

5 组测试（CRC、字序、RTU 组帧、TCP 联调、采集引擎）应全部 ✔，退出码 0。

---

## 2. 界面分区详解

```
┌──────────────────────────────────────────────────────────────────┐
│ [▶ 启动采集] [■ 停止] [⟳ 重载配置] [📁 数据目录]  未启动  设备：--  历史 0 条 │
├───────────────────────────────────────────┬──────────────────────┤
│ Tab「实时数据」                            │ 趋势图（手写 GDI+）   │
│  DataGridView：位号/值/单位/时间/质量/报警  │ 报警上下限虚线        │
│  （坏数据灰底、报警行红底）                 │ 自动量程              │
├───────────────────────────────────────────┴──────────────────────┤
│ Tab「报警记录」 / Tab「运行日志」                                   │
├──────────────────────────────────────────────────────────────────┤
│ 提示：双击数据目录可查看 CSV 历史；右键表格可写入寄存器              │
└──────────────────────────────────────────────────────────────────┘
```

| 控件 | 作用 |
|---|---|
| `▶ 启动采集` | 创建 `PollingService`、订阅事件、启动后台循环；运行中禁用 |
| `■ 停止` | `async` 停止，先退订事件再 `await StopAsync()`，UI 不假死 |
| `⟳ 重载配置` | 先停采集 → 重新读 `appsettings.json` → 重建表格行；运行中禁用 |
| `📁 数据目录` | 用资源管理器打开 CSV 所在目录 |
| `Tab 实时数据` | `DataGridView` 绑定 `BindingList<TagRow>` |
| `Tab 报警记录` | 超限触发 / 恢复各记一条 |
| `Tab 运行日志` | 最多保留 1000 行，自动滚到底 |
| 右键菜单 | `写入该点位（06/10 功能码）…`、`复制数值` |

> 构造函数里用**反射**打开了 `DataGridView.DoubleBuffered`（该属性是 protected），
> 否则 500ms 刷新一次会明显闪烁 —— WinForms 常见优化技巧。

---

## 3. 配置文件逐字段详解

文件：`项目/src/ModbusMonitor/config/appsettings.json`。
`ModbusMonitor.csproj` 里配了 `CopyToOutputDirectory=PreserveNewest`，
所以程序运行时读的是 `bin/Debug/net8.0-windows/config/appsettings.json` 的副本。

> 改配置有两种方式：① 改源码目录里的文件后重新 `dotnet run`；② 直接改输出目录里的副本（改完点「⟳ 重载配置」即可，但下次构建可能被覆盖）。推荐 ①。

### 3.1 顶层

| 字段 | 示例 | 说明 |
|---|---|---|
| `Title` | `"1# 锅炉监控上位机"` | 窗口标题 |
| `DataDirectory` | `"data"` | 历史 CSV 目录，相对 exe 所在目录 |
| `Devices` | `[...]` | 设备数组，每台设备一个后台采集循环 |

### 3.2 设备 `DeviceConfig`

| 字段 | 示例 | 说明 |
|---|---|---|
| `Name` | `"1#锅炉PLC"` | 设备名，也是表格里"设备"列的键（和位号拼成 `"设备\|位号"`） |
| `Enabled` | `true` | 关掉就不采集、不建表格行 |
| `Protocol` | `"ModbusTcp"` / `"ModbusRtu"` | 由 `ModbusMasterFactory` 决定创建哪个主站 |
| `SlaveId` | `1` | Modbus 站号 |
| `Host` / `Port` | `127.0.0.1` / `502` | TCP 模式用 |
| `PortName` / `BaudRate` / `DataBits` / `Parity` / `StopBits` | `COM3` / `9600` / `8` / `None` / `One` | RTU 模式用 |
| `PollIntervalMs` | `500` | 每台设备的采集周期（毫秒） |
| `Points` | `[...]` | 点位数组 |

### 3.3 点位 `PointConfig`

| 字段 | 示例 | 说明 |
|---|---|---|
| `Name` | `"炉膛温度"` | 位号名，界面显示 |
| `Address` | `0` | **0 基**寄存器地址（协议里 40001 对应 Address=0） |
| `PlcAddress` | `"40001"` | 仅用于显示/对照 PLC 手册 |
| `DataType` | `Int16` / `UInt16` / `UInt32` / `Float32` ... | 决定占几个寄存器、怎么解码 |
| `Order` | `"Abcd"` | 字序：ABCD / BADC / CDAB / DCBA（国产仪表常踩坑） |
| `Scale` | `0.1` | 工程值 = 原始值 × Scale + Offset |
| `Offset` | `0` | 零点偏移 |
| `Unit` | `"℃"` | 单位 |
| `LowLimit` / `HighLimit` | `20` / `950` | 报警下限/上限；不填就不判断 |
| `Enabled` | 默认 true | 关掉即不采集 |
| `Description` | 任意文本 | 备注 |

### 3.4 加一个点位（不用改代码）

在 `Points` 数组里复制一段，例如新增"排烟温度"：

```json
{
  "Name": "排烟温度",
  "Address": 5,
  "PlcAddress": "40006",
  "DataType": "UInt16",
  "Scale": 0.1,
  "Unit": "℃",
  "HighLimit": 300
}
```

保存 → 点「⟳ 重载配置」→「▶ 启动采集」。表格自动多一行，趋势图可选中它。
**这就是"配置驱动"：新增设备/点位不动一行 C# 代码。**

### 3.5 启用 RTU 设备

配置里第二台 `2#液位计（示例，默认关闭）` 默认 `Enabled: false`。改成 `true` 并把 `PortName` 改成你的串口：

| 端口来源 | 填什么 |
|---|---|
| 真实 USB 转 485 | `COM3` |
| com0com 虚拟对 | `COM5`（另一端跑 RTU 从站） |
| **VComPair** | **不支持**：`ModbusMasterFactory` 创建的是 `SerialPortNative`，只认物理/驱动 COM 口；想走 VCOM 需要扩展工厂（见扩展任务） |

> 仓库自带的 `tools/modbus_slave_sim.py` 是 **TCP** 从站，不直接走串口。RTU 联调请配合
> [`../com0com虚拟串口工具/04-串口调试助手联调实战.md`](../com0com虚拟串口工具/04-串口调试助手联调实战.md) 里的模拟从站思路。

---

## 4. 采集全链路（重点）

```
config/appsettings.json
      │ JsonConfigStore.Load
      ▼
   AppConfig ──► PollingService.Start()
                   │ 每台启用设备一个 Task
                   ▼
              PollLoopAsync(device)
                   │ ① ModbusMasterFactory.Create → ConnectAsync
                   │ ② BuildGroups：相邻地址合并，≤125 寄存器/帧
                   │ ③ ReadHoldingRegistersAsync(03)
                   │ ④ Decode：字序/数据类型 → 工程值 → 报警级别
                   │ ⑤ RaiseSnapshot（后台线程）
                   ▼
        SnapshotReady 事件 ──► MainForm.OnSnapshotReady
                                  │ BeginInvoke 切回 UI 线程
                                  ▼
                            ApplySnapshot
                              ├─ 更新 TagRow（BindingList 自动刷新表格）
                              ├─ 好数据入 ConcurrentQueue → 定时批量写 CSV
                              ├─ 选中位号 → TrendChart.AddPoint
                              └─ 报警状态翻转 → 插入报警记录
                   │
                   └─ 异常/掉线：BuildBadSnapshot（全部点位 Quality=Bad）
                      → 断开 → 等 2 秒重连（DeviceStateChanged 报状态）
```

### 4.1 为什么要批量读

`PollingService.BuildGroups()` 把地址相邻的点合并：

```
点位地址 100、101、102、105  →  合成 [100..105] 一次读 6 个寄存器
```

合并条件（源码注释写得很清楚）：

1. 和上一块起点不重叠；
2. 地址间隔 ≤ 2 个寄存器（间隔太大不值得多读）；
3. 合并后总长度 ≤ **125**（Modbus 单帧读保持寄存器的上限）。

中间空地址读回来直接丢掉 —— 多读 2 个寄存器的代价远小于多发一帧。
**20 个连续点位从 20 帧降到 1~2 帧**，这是 485 总线上最实在的优化，也是简历里的亮点。

### 4.2 解码与报警

```csharp
var raw  = RegisterCodec.Decode(regs.AsSpan(offset, count), point.DataType, point.Order);
var eng  = RegisterCodec.ToEngineering(raw, point.Scale, point.Offset);
var alarm = eng > high ? AlarmLevel.High : eng < low ? AlarmLevel.Low : AlarmLevel.None;
```

`Math.Round(engineering, 3)` 后打包成 `TagValue`（设备名、位号、值、单位、时间戳、质量、报警）。

### 4.3 故障降级

任何一步失败（连不上、超时、CRC 错）→ `BuildBadSnapshot()` 给该设备**每个点位**生成 `Quality.Bad` 的记录 →
界面把行灰掉、不写 CSV、不画曲线，然后断开连接、等 2 秒重连。

> 这是工业软件的关键原则：**宁可显示"数据失效"，也不能让操作工看着过期数值做决策。**

---

## 5. 代码逐段讲解

| # | 位置 | 讲什么 |
|---|---|---|
| 1 | `MainForm` 构造函数 | 反射开 `DoubleBuffered`；`gridTags.DataSource = _tags`（`BindingList<TagRow>` 自动通知刷新） |
| 2 | `MainForm_Load` | `Log.Init(logs)` → 订阅 `Log.MessageWritten` → `LoadConfig()` → `tmrFlush.Start()` |
| 3 | `LoadConfig` | `JsonConfigStore.Load(path, CreateDefaultConfig)`；**配置坏了不让程序起不来**：记日志 + 弹窗 + 退回内置默认配置 |
| 4 | `BuildRows` | 只遍历 `Enabled` 的设备/点位，构造 `TagRow`，建 `_tagIndex` 字典（键 `"设备\|位号"`），默认选中第一行 |
| 5 | `StartCollection` | `new PollingService(_config)` → 订阅 `SnapshotReady` / `DeviceStateChanged` → `Start()`；切按钮状态 |
| 6 | `StopCollectionAsync` | **先退订事件**、再 `await service.StopAsync()`，最后恢复按钮；`async void` 只用于事件处理器 |
| 7 | `OnSnapshotReady` | 后台线程回调，`BeginInvoke(new Action<IReadOnlyList<TagValue>>(ApplySnapshot), values)`；捕获 `ObjectDisposedException` / `InvalidOperationException` |
| 8 | `ApplySnapshot` | 更新行 → 好数据入 `_history` 队列 → 选中位号加曲线点 → **报警边沿检测** |
| 9 | `gridTags_SelectionChanged` | 切换位号时 `trendChart.Reset(标题, 单位, 低限, 高限)` |
| 10 | `gridTags_CellFormatting` | 坏数据灰底、报警行红底（只做轻量判断，因为每个单元格都会调） |
| 11 | `miWrite_Click` | 工程值 → `ToRaw` → `Encode` → **独立连接** `WriteSingleRegisterAsync(06)` / `WriteMultipleRegistersAsync(10)` |
| 12 | `FlushHistory` | 一次最多取 5000 条批量 `Append`；写盘失败只记日志，绝不让采集停掉 |
| 13 | `MainForm_FormClosing` | 停采集（`.Wait(1500)` 带超时防串口卡死）→ 把队列剩余数据落盘 → 写退出日志 |

### 5.1 报警状态机（`ApplySnapshot` 里的关键几行）

```csharp
var previous = _alarmState.TryGetValue(key, out var level) ? level : AlarmLevel.None;
if (previous == v.Alarm) continue;          // 状态没变 → 不重复记
_alarmState[key] = v.Alarm;
if (v.Alarm != AlarmLevel.None)  _alarms.Insert(0, new AlarmRecord(..., "触发"));
else if (previous != AlarmLevel.None) _alarms.Insert(0, new AlarmRecord(..., "恢复"));
while (_alarms.Count > 500) _alarms.RemoveAt(_alarms.Count - 1);
```

**只在状态翻转时记一条**。超限持续 10 分钟也只产生一条"触发"，恢复时再记一条"恢复"，不会每 500ms 刷屏。
这是报警系统设计的核心，面试很爱问。

### 5.2 为什么历史数据要过队列

采集线程 500ms 一轮，如果每轮直接写文件，磁盘 IO 会拖慢采集甚至造成抖动。
所以：`ApplySnapshot` 只 `ConcurrentQueue.Enqueue`（O(1)），
`tmrFlush_Tick`（默认 1 秒）批量 `TryDequeue` 至多 5000 条再一次性 `Append`。
采集线程与 UI 线程都会入队，`ConcurrentQueue` 保证线程安全；`CsvHistoryStore` 内部还有 `lock` 串行化写文件。

---

## 6. 历史 CSV

- 位置：`项目/src/ModbusMonitor/bin/Debug/net8.0-windows/data/yyyyMMdd.csv`（点「📁 数据目录」直接打开）
- 按天滚动：`20261109.csv`、`20261110.csv` …
- 编码：**UTF-8 带 BOM**，Excel 双击不乱码；小数用 `InvariantCulture`，不会因系统区域设置变成逗号
- 表头：

```
时间,设备,位号,数值,单位,质量,报警
```

- 只有 `Quality == Good` 的数据才入库（坏数据会污染历史曲线）
- `FileShare.Read` 允许写的同时用 Excel 打开查看；整批写完统一 `Flush()`，断电最多丢最后一行
- 含逗号的字段会加引号转义

---

## 7. 写入寄存器完整流程

1. 在「实时数据」表格**右键**任意一行 → `写入该点位（06/10 功能码）…`；
2. `InputDialog` 弹出，提示"请输入工程值（℃）"，默认值取当前行；
3. 输入 `900` → 确定；
4. 代码做三步转换：

```csharp
var raw        = RegisterCodec.ToRaw(engineering, point.Scale, point.Offset); // 900 / 0.1 = 9000
var registers  = RegisterCodec.Encode(raw, point.DataType, point.Order);       // 按类型/字序编码
using var master = ModbusMasterFactory.Create(device);                         // 独立连接
await master.ConnectAsync();
// 1 个寄存器 → 06；多个寄存器 → 10
await master.WriteSingleRegisterAsync(device.SlaveId, point.Address, registers[0]);
```

5. 状态栏提示"写入成功"，日志记一条；下次采集读回的值应该跟着变。

> **为什么用独立连接**：采集引擎正占着一条 TCP 连接周期读，写操作另开一条，避免和采集抢同一条链路导致帧交错。
> 生产项目还要加：权限校验、写入确认、读回校验、操作审计。

---

## 8. 排错表

| 现象 | 检查顺序 |
|---|---|
| 点启动采集没反应 / 表格全是 `--` | 模拟器是否在运行 → 端口 502 是否被占用 → 配置 `Host/Port/SlaveId` → 看「运行日志」Tab |
| 全部行变灰、质量 Bad | 设备掉线，引擎在 2 秒后自动重连；看日志里的异常原因（连接被拒 / 超时 / CRC） |
| 数值明显不对 | 先查 `Scale`（0.1 还是 0.01）→ 再查 `Order` 字序（ABCD/BADC/CDAB/DCBA）→ 再查 `DataType`（Int16 有符号 vs UInt16 无符号） |
| 报警一直刷屏 | 检查上下限是否配得太贴近正常波动；正常逻辑只在状态翻转时记一条 |
| 曲线不动 | 是否选中了某行 → 该点位是否有 `Quality.Good` 数据 → 采集是否启动 |
| 改了配置没生效 | 改的是源码目录还是输出目录？点「⟳ 重载配置」；运行中该按钮禁用，先停采集 |
| CSV 没生成 | 点「📁 数据目录」看路径；确认运行目录有写权限；看日志有没有"历史数据写入失败" |
| 写入报错 | 站号/地址是否越界；从站是否支持 06/10；Float32/UInt32 会走 10 功能码 |
| 502 端口冲突 | 换 `--port 1502`，同步改配置里的 `Port` |
| 关闭窗口卡住 | `StopAsync().Wait(1500)` 有超时保护；若仍卡，检查串口是否被别的程序占用 |
| 表格闪烁 | 构造函数里已反射开启 `DoubleBuffered`，别误删 |

---

## 9. 简历 / 面试表达

| 能讲的点 | 一句话 |
|---|---|
| 手写 Modbus 主站 | RTU/TCP 帧格式、CRC16、MBAP、异常码、超时与串行化，全零第三方依赖 |
| 批量读优化 | 地址合并，20 个点位从 20 帧降到 1~2 帧，485 总线负载大幅下降 |
| 故障降级 | 通信失败把整台设备打 Bad，界面不显示过期数据，自动重连 |
| 线程模型 | 每台设备一个后台 Task，UI 只订阅事件并 `BeginInvoke`，通信卡死界面不假死 |
| 存储解耦 | 采集线程只入 `ConcurrentQueue`，定时批量落盘，磁盘 IO 不影响采集周期 |
| 配置驱动 | 新增设备/点位只改 JSON，不动 C# 代码 |
| 报警状态机 | 只在状态翻转时记触发/恢复，避免刷屏 |
| 工程值换算 | Scale/Offset + 四种字序 + Int16/UInt16/UInt32/Float32 编解码 |

---

## 10. 扩展任务

- [ ] CSV 换 SQLite，增加"按时间/位号查询历史"页面（见 `文档/09`）
- [ ] 报警确认 / 复位 + 操作人 + 权限（见 `文档/11`）
- [ ] 用户登录（操作员只读、工程师可写）
- [ ] 配方下发：选配方 → 批量写 → **读回校验**
- [ ] 班报 / 日报导出 Excel
- [ ] 断线缓存与恢复后补传
- [ ] 多设备并行 + 设备树导航
- [ ] 让 `ModbusMasterFactory` 支持 `VCOMx`，使项目 2 也能用 VComPair 免驱动练 RTU
- [ ] 采集引擎加 `Channel<T>` 解耦，替换事件回调（见 `文档/10`）

---

## 11. 验收清单

- [ ] 能在无 PLC 的情况下跑通全链路（模拟器 + 上位机）
- [ ] 能手工加一个点位并看到表格/曲线变化
- [ ] 能说清 `BuildGroups` 的三条合并条件与 125 上限的由来
- [ ] 能解释为什么坏数据要上报 Bad、为什么不入 CSV、为什么不画曲线
- [ ] 能复述报警"只在状态翻转时记一条"的实现
- [ ] 能讲清 `ConcurrentQueue` + 定时批量落盘的动机
- [ ] 能独立完成一次写寄存器并读回验证
- [ ] 能定位字序 / Scale / DataType 导致的数值错误

---

**下一步**：[19 · 项目三 WPF 监控看板完全教程](19-项目三WPF监控看板完全教程.md)
