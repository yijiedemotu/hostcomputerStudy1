# 项目源码说明

本目录是可编译运行的完整示例，配套教程在上级 `文档/`。
`src/` 下**只保留三个教学项目**（外加它们共用的类库 `Industrial.Core`）；课后练习答案已移到上级独立的 `练习题答案/` 解决方案。

## 结构

```
项目/
├── UpperComputer.Learning.slnx
├── src/
│   ├── Industrial.Core/     类库：Modbus RTU/TCP、CRC、字序、串口、虚拟串口、配置、CSV、日志、采集引擎
│   │                        （被三个 UI 项目引用，不是独立教学项目，不能删）
│   ├── SerialAssistant/     项目 1：串口调试助手（WinForms）
│   ├── ModbusMonitor/       项目 2：Modbus 采集监控上位机（WinForms）
│   └── ScadaDashboard/      项目 3：WPF 监控看板（MVVM）
└── tools/
    ├── SelfCheck/           协议自检（控制台）
    └── modbus_slave_sim.py  Modbus TCP 从站模拟器
```

依赖方向：`UI → Industrial.Core`，`Core` 不引用任何 UI。

配套工具（不在本目录）：上级 `com0com虚拟串口工具/`，提供免驱动虚拟串口对 VCOM1 ↔ VCOM2。

## 构建

```powershell
cd 项目
dotnet build src\Industrial.Core\Industrial.Core.csproj
dotnet build src\SerialAssistant\SerialAssistant.csproj
dotnet build src\ModbusMonitor\ModbusMonitor.csproj
dotnet build src\ScadaDashboard\ScadaDashboard.csproj
dotnet build tools\SelfCheck\SelfCheck.csproj
```

一次构建整个解决方案：

```powershell
dotnet build UpperComputer.Learning.slnx -p:UseSharedCompilation=false
```

> 在受限沙箱/CI 里，MSBuild 共享编译进程可能连不上命名管道，表现为"生成失败，0 个错误"。
> 加 `-p:UseSharedCompilation=false` 或逐项目构建即可。

## 运行

```powershell
# 终端 1：Modbus TCP 从站模拟器（项目 2 / 3 需要）
python tools\modbus_slave_sim.py

# 终端 2：串口通讯（项目 1 需要，免驱动、不用装 com0com）
dotnet run --project ..\com0com虚拟串口工具\VComPair

dotnet run --project tools\SelfCheck -- 502   # 自检 + 联调
dotnet run --project src\SerialAssistant       # 端口选 VCOM1
dotnet run --project src\ModbusMonitor
dotnet run --project src\ScadaDashboard
```

> 串口助手也可以用真实串口 / com0com 创建的 COM5 ↔ COM6；
> 用 VCOM1 / VCOM2 时先启动 VComPair 工具，详见
> [`../com0com虚拟串口工具/06-VComPair免驱动工具完全教程.md`](../com0com虚拟串口工具/06-VComPair免驱动工具完全教程.md)。

## 三个项目的详细教程

| 项目 | 教程 |
|---|---|
| 项目 1 串口调试助手 | [`../文档/17-项目一串口调试助手完全教程.md`](../文档/17-项目一串口调试助手完全教程.md) |
| 项目 2 Modbus 监控上位机 | [`../文档/18-项目二Modbus采集监控完全教程.md`](../文档/18-项目二Modbus采集监控完全教程.md) |
| 项目 3 WPF 监控看板 | [`../文档/19-项目三WPF监控看板完全教程.md`](../文档/19-项目三WPF监控看板完全教程.md) |

每个项目目录下另有独立 `README.md`，写明功能、关键文件与扩展任务。

## 课后练习答案（独立解决方案）

教程第 01、02、04、05、07、10 章的 33 道练习答案在**上级目录的 `练习题答案/`**，与本解决方案完全分离：

```powershell
cd ..\练习题答案
dotnet run --project src\Answers.Console -- all   # 02 / 07 / 10 章
dotnet run --project src\Answers.WinForms         # 01 / 04 章
dotnet run --project src\Answers.Wpf              # 05 章
```

题目与代码逐条对照见 `练习题答案/README.md`。各章文档里的引用也都指向这个独立目录。

## 技术要点

- 目标框架：`net8.0-windows`（WinForms/WPF）、`net8.0`（Core、SelfCheck）
- 零 NuGet 依赖：串口用 Win32 API，图表用 GDI+/DrawingContext 手写
- 虚拟串口：`Industrial.Core/Transport/VirtualSerialPort.cs` 实现同一个 `ISerialPort`，走本机 TCP 回环
- 配置：`src/ModbusMonitor/config/appsettings.json`
- 数据：`bin/.../data/yyyyMMdd.csv`，日志：`bin/.../logs/`

## 建议的阅读顺序

1. `Industrial.Core/Modbus/Crc16.cs` → `RegisterCodec.cs` → `ModbusTcpMaster.cs` → `ModbusRtuMaster.cs`
2. `Industrial.Core/Transport/ISerialPort.cs` → `SerialPortNative.cs` → `VirtualSerialPort.cs`（抽象与两种实现）
3. `Industrial.Core/Services/PollingService.cs`（批量读、取消、故障降级）
4. `SerialAssistant/MainForm.cs`（后台接收 + UI 更新）
5. `ModbusMonitor/MainForm.cs` + `TrendChart.cs`
6. `ScadaDashboard/**`（MVVM）

## 免责

仅用于学习。控制真实设备前必须做安全评估、联锁与权限控制。
