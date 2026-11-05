# 课后练习答案

本目录把教程里**有练习的 6 个章节**的答案集中成三个可运行项目，共 **33 道题**。每道题都是独立文件，代码里写满了中文注释，可以按题号直接翻看：

| 项目 | 覆盖章节 | 题目数 | 形态 |
|---|---|---|---|
| `Exercises.ConsoleApp` | 第 02、07、10 章 | 7 + 6 + 5 = 18 | 控制台，逐题打印答案与解释 |
| `Exercises.WinForms` | 第 01、04 章 | 5 + 5 = 10 | 6 个可操作页签 |
| `Exercises.Wpf` | 第 05 章 | 5 | 左侧 5 个练习页，支持日夜换肤 |

没有把所有答案塞进一个 exe，是因为练习分别要求控制台、WinForms、WPF 三种技术栈。拆成三个项目后，每道题都能直接运行、直接看效果，也方便对照章节学习。

## 快速开始

前置条件：Windows 10/11、.NET 8 SDK（含 WindowsDesktop 运行时）。以下命令都在 `项目` 目录执行。

```powershell
cd 项目

# 第 02 / 07 / 10 章：控制台菜单
dotnet run --project src\Exercises\Exercises.ConsoleApp

# 也可以直接跑某一章，或一次跑完
dotnet run --project src\Exercises\Exercises.ConsoleApp -- 2
dotnet run --project src\Exercises\Exercises.ConsoleApp -- 7
dotnet run --project src\Exercises\Exercises.ConsoleApp -- 10
dotnet run --project src\Exercises\Exercises.ConsoleApp -- all

# 第 01 / 04 章：WinForms 六个页签
dotnet run --project src\Exercises\Exercises.WinForms

# 第 05 章：WPF 五个练习页
dotnet run --project src\Exercises\Exercises.Wpf
```

> `--` 后面才是传给程序的参数。直接写 `dotnet run ... 2` 会被 `dotnet run` 当成项目参数。

## 题目与代码对照

### 控制台：第 02 章（7 题）

| 题 | 练习内容 | 答案位置 |
|---|---|---|
| 02-1 | `ParseHex`：十六进制字符串转 `byte[]`，非法字符抛异常 | `Chapter02/Exercise01_ParseHex.cs` |
| 02-2 | `ToHex`：字节数组转带空格的十六进制字符串 | `Chapter02/Exercise02_ToHex.cs` |
| 02-3 | LINQ：筛选温度 > 100 且质量正常的位号 | `Chapter02/Exercise03_Linq.cs` |
| 02-4 | `PointConfig` 的 JSON 序列化 / 反序列化 | `Chapter02/Exercise04_Json.cs` |
| 02-5 | `TemperatureChanged` 事件订阅与退订 | `Chapter02/Exercise05_Event.cs` |
| 02-6 | `async Task<int>`：1 秒后返回 42 | `Chapter02/Exercise06_Async.cs` |
| 02-7 | 故意制造空引用异常，用断点定位 | `Chapter02/Exercise07_NullReference.cs` |

每章的 `Chapter02.cs`、`Chapter07.cs`、`Chapter10.cs` 只是“目录”，按顺序调用各题；看答案时直接打开对应题号的文件即可。

### 控制台：第 07 章（6 题）

| 题 | 练习内容 | 答案位置 |
|---|---|---|
| 07-1 | 手发 RTU 帧，并用模拟器验证链路 | `Chapter07/Exercise01_SendFrame.cs` |
| 07-2 | 解析 4 个寄存器的应答帧并校验 CRC | `Chapter07/Exercise02_ParseResponse.cs` |
| 07-3 | 手算 `01 06 00 05 03 78` 的 CRC | `Chapter07/Exercise03_Crc.cs` |
| 07-4 | 同一组寄存器在四种字序下的数值 | `Chapter07/Exercise04_RegisterOrder.cs` |
| 07-5 | 采集周期改成 100 ms，测量快照间隔 | `Chapter07/Exercise05_Polling100ms.cs` |
| 07-6 | 连续失败 3 次自动重连的主站装饰器 | `Chapter07/Exercise06_Reconnect.cs` |

### 控制台：第 10 章（5 题）

| 题 | 练习内容 | 答案位置 |
|---|---|---|
| 10-1 | `PeriodicTimer` 与 `Task.Delay` 的周期精度对比 | `Chapter10/Exercise01_TimerComparison.cs` |
| 10-2 | `Channel` 解耦“采集 100 ms、批量存盘 1 s” | `Chapter10/Exercise02_ChannelDecoupling.cs` |
| 10-3 | 复现 `.Result` 死锁并给出两种修复 | `Chapter10/Exercise03_Deadlock.cs` |
| 10-4 | 连续失败 3 次才离线，成功 1 次立即恢复 | `Chapter10/Exercise04_FailureThreshold.cs` |
| 10-5 | 关闭前把队列里的历史数据全部落盘 | `Chapter10/Exercise05_FlushOnClose.cs` |

### WinForms：第 01 / 04 章（10 题）

| 题 | 练习内容 | 答案位置 |
|---|---|---|
| 01 | 第一个小任务：大写转换、空输入提示、勾选变红 | `Views/FirstTaskPanel.cs` |
| 04-1 | 发送历史（最近 10 条 + JSON 持久化） | `Views/SendHistoryPanel.cs` |
| 04-2 | 表格按报警状态筛选 | `Views/AlarmFilterPanel.cs` |
| 04-3 | 趋势图鼠标悬停显示数值 | `Views/TrendHoverPanel.cs` + `Controls/HoverTrendChart.cs` |
| 04-4 | `VirtualMode` 加载 10 万行历史数据 | `Views/VirtualGridPanel.cs` + `Controls/BufferedDataGridView.cs` |
| 04-5 | 关闭窗口最小化到托盘，双击恢复 | `Views/TrayPanel.cs` + `Controls/TrayDemoForm.cs` |

发送历史保存在 `%LOCALAPPDATA%\UpperComputerLearning\ch04-send-history.json`，删掉该文件即可恢复初始示例。托盘练习点击“打开演示窗口”后，点演示窗口的关闭按钮试试隐藏与恢复。

### WPF：第 05 章（5 题）

| 题 | 练习内容 | 答案位置 |
|---|---|---|
| 05-1 | 报警声音、静音与确认复位 | `Views/AlarmSoundView.xaml` + `ViewModels/AlarmSoundViewModel.cs` |
| 05-2 | 趋势图十字光标与“时间 + 数值”提示框 | `Views/CrosshairView.xaml` + `Controls/CrosshairTrendChart.cs` |
| 05-3 | `DataTemplateSelector` 区分 PLC / 电表 / 变频器卡片 | `Views/DeviceCardsView.xaml` + `Selectors/DeviceTemplateSelector.cs` |
| 05-4 | 日间 / 夜间主题切换 | `Views/ThemeView.xaml` + `Services/ThemeManager.cs` + `Themes/*.xaml` |
| 05-5 | `CollectionViewSource` 报警排序与筛选 | `Views/AlarmListView.xaml` + `ViewModels/AlarmListViewModel.cs` |

WPF 答案的重点是分层：定时器、声音播放放在 View 的后台代码里；报警状态、筛选排序、命令放在 ViewModel；颜色和控件模板放在 `Themes/`。换肤使用 `DynamicResource`，切换时替换合并字典。

## 联调 Modbus 模拟器

第 07 章练习 1 会尝试连接 `127.0.0.1:502`。没有模拟器时程序会打印提示并继续，其他题目不依赖网络。要验证真实应答：

```powershell
# 终端 1（仍在 项目 目录）
python tools\modbus_slave_sim.py --port 1502 --verbose

# 终端 2
$env:MODBUS_PORT = "1502"
dotnet run --project src\Exercises\Exercises.ConsoleApp -- 7
```

端口 502 空闲时可以省略 `--port 1502`，程序默认连 502。模拟器寄存器 0 是模温（0.1 ℃）、寄存器 1 是注射压力（0.01 MPa），数值会随时间变化。

## 与三个完整项目的关系

- `Exercises.ConsoleApp` 引用 `Industrial.Core`，复用 CRC、字序、采集引擎等实现。
- `Exercises.WinForms` 和 `Exercises.Wpf` 不引用 `Industrial.Core`，各自保持单项目可运行，方便只看界面章节的读者。
- 完整项目仍然是 `SerialAssistant`、`ModbusMonitor`、`ScadaDashboard`。练习答案只回答章节末尾的小题，规模和业务完整度都不如那三个项目；写简历时以完整项目为主。

## 注意

- WinForms / WPF 必须在 Windows 交互式桌面运行；控制台项目可在其他平台运行。
- 答案是教学代码，故意保留了一些“先演示错误、再修复”的段落，例如第 10 章的 `.Result` 死锁。
- 第 07 章应答示例的 CRC 是 `90 08`（低字节 `90` 在前）。旧笔记如果写成 `BA 7A`，以 `90 08` 为准。
- 涉及真实 PLC / 设备的写操作，必须先在联锁、急停和权限完备的环境中评估。
