# 03 工程化：分层、Git、日志与测试

> 本章目标：从“能写”到“能交付”。企业看的是你的代码能不能被别人接手、出问题能不能查。
> 这一章决定了你是 6k 还是 15k。

---

## 1. 解决方案长什么样

本仓库的 `项目/`：

```
项目/
├── UpperComputer.Learning.sln
├── src/
│   ├── Industrial.Core/          # 类库：协议 + 模型 + 存储 + 日志（无 UI 依赖）
│   ├── SerialAssistant/          # WinForms：串口调试助手
│   ├── ModbusMonitor/            # WinForms：采集监控上位机
│   └── ScadaDashboard/           # WPF：监控看板
└── tools/
    ├── SelfCheck/                # 控制台：协议自检
    └── modbus_slave_sim.py       # Python：从站模拟器
```

依赖方向（**只能向下，不能反向**）：

```
        UI (WinForms / WPF)
                │
                ▼
        Industrial.Core
        ├── Modbus（协议）
        ├── Transport（串口）
        ├── Models（配置/数据）
        ├── Storage（CSV/JSON）
        └── Services（采集引擎）
```

为什么 UI 不能反过来被 Core 引用？
→ Core 一旦 `using System.Windows.Forms`，就没法在控制台/服务/测试里复用了。

**判断分层好坏的标准：能不能把 Core 单独拿去做一个 Windows 服务？** 本仓库可以。

---

## 2. 一个类库该放什么

| 目录 | 内容 | 例子 |
|---|---|---|
| `Modbus/` | 协议实现 | `ModbusTcpMaster`、`Crc16` |
| `Transport/` | 物理通信抽象 | `ISerialPort`、`SerialPortNative` |
| `Models/` | 纯数据 | `AppConfig`、`TagValue` |
| `Storage/` | 持久化 | `CsvHistoryStore`、`JsonConfigStore` |
| `Services/` | 业务流程 | `PollingService` |
| `Diagnostics/` | 日志、诊断 | `Log` |

命名空间与目录一致：`Industrial.Core.Modbus` 对应 `Modbus/` 文件夹。

---

## 3. 配置管理：不要把参数写死在代码里

反例：

```csharp
// 现场换个 IP 就要重新编译发版？客户会骂人的
var client = new TcpClient("192.168.1.10", 502);
```

正确做法：**JSON 配置 + 强类型 + 默认值 + 容错**。

`config/appsettings.json`：

```json
{
  "Title": "1# 锅炉监控上位机",
  "DataDirectory": "data",
  "Devices": [
    {
      "Name": "1#锅炉PLC",
      "Protocol": "ModbusTcp",
      "Host": "127.0.0.1",
      "Port": 502,
      "SlaveId": 1,
      "PollIntervalMs": 500,
      "Points": [
        { "Name": "炉膛温度", "Address": 0, "DataType": "Int16", "Scale": 0.1, "Unit": "℃", "HighLimit": 950 }
      ]
    }
  ]
}
```

读取：

```csharp
var config = JsonConfigStore.Load(path, CreateDefaultConfig);
```

`JsonConfigStore` 做了三件保命的事：

1. 文件不存在 → 生成默认配置，程序照样能启动；
2. 格式错误 → 备份坏文件，抛带路径的异常，不静默；
3. 保存时先写 `.tmp` 再替换，避免断电写坏配置。

> 进阶：配置加密（密码、数据库连接串）用 `ProtectedData`（DPAPI），只对当前用户/机器可解。

---

## 4. 日志：现场唯一能替你说话的东西

```csharp
Log.Info($"已连接 {device.Endpoint}");
Log.Warn($"应答超时：站号 {unitId}，功能码 0x03");
Log.Error("采集失败", ex);
```

规范：

| 级别 | 用在哪 | 例子 |
|---|---|---|
| INFO | 关键流程节点 | 启动、连接、配置加载、开始/停止采集 |
| WARN | 可恢复的异常 | 超时一次、重试、报警触发 |
| ERROR | 影响功能 | 连接失败、数据库写失败、未处理异常 |

必须包含的上下文：**时间、设备名/站号、功能码/地址、原始报文、异常堆栈**。

日志落盘策略：

- 按天滚动：`logs/20261018.log`
- 保留 30 天，自动清理
- 不要每个字节都打日志（会拖垮性能、写满硬盘）
- 关键报文用十六进制：`01 03 00 00 00 0A C5 CD`

> 生产项目推荐 Serilog（结构化日志、异步写、多 Sink）。本仓库的 `Log` 类手写了同样的核心思想，便于理解。

---

## 5. Git：不会 Git 等于不会编程

### 5.1 最小命令集

```bash
git init
git add .
git commit -m "feat: 完成 Modbus TCP 主站"
git status
git log --oneline --graph
git diff

git branch feature/alarm
git checkout feature/alarm
git merge main

git remote add origin https://github.com/you/repo.git
git push -u origin main
```

### 5.2 .gitignore（上位机项目必备）

```gitignore
bin/
obj/
.vs/
*.user
*.suo
data/
logs/
publish/
```

**永远不要提交**：编译产物、日志、历史数据、包含密码的配置。

### 5.3 提交信息规范

```
feat: 新增报警记录表
fix: 修复 RTU 应答 CRC 校验失败
refactor: 抽出 IModbusMaster 接口
docs: 补充部署说明
test: 增加字序转换测试
```

一个提交只做一件事。**面试官看你的 GitHub，第一眼看提交记录。**

---

## 6. 依赖注入：别在类里 new 依赖

反例：

```csharp
public class PollingService
{
    private readonly ModbusTcpMaster _master = new("127.0.0.1", 502);  // 写死，无法测试
}
```

正确：

```csharp
public PollingService(AppConfig config, Func<DeviceConfig, IModbusMaster>? factory = null)
{
    _config = config;
    _factory = factory ?? (d => ModbusMasterFactory.Create(d));
}
```

好处：

1. 测试时传入假实现（SelfCheck 就这么干的）；
2. 换实现不改调用方；
3. 生命周期可控。

手写一个极简容器（理解原理即可）：

```csharp
//作用：一个注册表，记录【某个接口类型】该**怎么实例化对象**。
//✅ 关键点：现在**还没有创建 Modbus 对象**，只是把「创建规则」存进去。
var services = new Dictionary<Type, Func<object>>();
//- 当我调用这个函数的时候，就 new 一个 ModbusTcpMaster 实例（TCP Modbus 主站，地址 127.0.0.1:502）  前提：`ModbusTcpMaster` **实现了 IModbusMaster 接口**
services[typeof(IModbusMaster)] = () => new ModbusTcpMaster("127.0.0.1", 502);
var master = (IModbusMaster)services[typeof(IModbusMaster)]();
//`(IModbusMaster)`：强制转型，因为委托返回值是 `object`，转回接口类型
```

生产项目用 `Microsoft.Extensions.DependencyInjection`。

---

## 7. 单元测试：测协议，不测界面

上位机最值得测的是**纯逻辑**：

- CRC 计算
- 字节序转换
- 报文组帧/解析
- 工程量换算
- 报警判断
- 批量读分组算法

```csharp
[Fact]
public void Crc16_KnownFrame_ReturnsExpected()
{
    var crc = Crc16.Compute(new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x0A });
    Assert.Equal(0xCDC5, crc);
}
```

界面测试成本高、收益低，**优先把逻辑从界面里抽出来**，这就是分层的回报。

本仓库的 `tools/SelfCheck` 是不依赖测试框架的自检程序，思路一样：

```
已知报文 → 期望结果 → 断言 → 退出码
```

它还有个隐藏好处：**面试时可以现场跑给面试官看。**

---

## 8. 性能与内存：三个高频问题

### 8.1 界面卡顿

原因：UI 线程做了耗时操作（`Thread.Sleep`、同步读串口、大量字符串拼接）。
解决：耗时操作放后台，UI 只负责显示。

### 8.2 内存持续增长

原因：事件订阅未取消、无限增长的 `List`、`DataGridView` 行只增不减、日志缓存队列无上限。
排查：任务管理器 → 诊断工具 → 快照对比。

### 8.3 CPU 占用高

原因：`while(true)` 空转、`Timer` 间隔过短、每帧重绘整个界面。
解决：读超时 + `Task.Delay`、降低刷新频率、只重绘脏区。

```csharp
// 错误：空转吃满一个核
while (running) { if (port.HasData) Read(); }

// 正确：带超时的阻塞读
while (running)
{
    var n = port.Read(buffer, 0, buffer.Length, 50);
    if (n > 0) Process(buffer, n);
}
```

---

## 9. 代码评审清单（提交前自查）

- [ ] 没有 `catch { }` 空吞异常
- [ ] 没有魔法数字（`502`、`9600` 应来自配置）
- [ ] 没有在 UI 线程做阻塞操作
- [ ] 所有 `IDisposable` 都释放了
- [ ] 事件订阅有对应的退订
- [ ] 日志带上下文，不打印敏感信息
- [ ] 配置有默认值，缺字段不崩
- [ ] 方法不超过 50 行，类不超过 500 行
- [ ] 命名能看懂（`ReadHoldingRegisters` 而不是 `DoIt`）
- [ ] 新增逻辑有对应测试或自检

---

## 10. 本章验收

- [ ] 能画出你项目的分层图，并说明依赖方向
- [ ] 把项目里至少 3 个硬编码参数挪到 JSON 配置
- [ ] 用 `Log` 记录启动、连接、采集失败、退出四类事件
- [ ] 初始化 Git 仓库，提交记录不少于 10 条
- [ ] 为 CRC 或字序写一个自检/单测
- [ ] 用任务管理器观察你的程序跑 10 分钟，内存是否稳定

下一章：`04 WinForms 界面开发实战.md`
