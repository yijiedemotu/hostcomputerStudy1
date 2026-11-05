# 项目源码说明

本目录是可编译运行的完整示例，配套教程在上级 `文档/`。

## 结构

```
项目/
├── UpperComputer.Learning.slnx
├── src/
│   ├── Industrial.Core/     类库：Modbus RTU/TCP、CRC、字序、串口、配置、CSV、日志、采集引擎
│   ├── SerialAssistant/     项目 1：串口调试助手（WinForms）
│   ├── ModbusMonitor/       项目 2：Modbus 采集监控上位机（WinForms）
│   ├── ScadaDashboard/      项目 3：WPF 监控看板（MVVM）
│   └── Exercises/           课后练习答案：Console（02/07/10）、WinForms（01/04）、WPF（05）
└── tools/
    ├── SelfCheck/           协议自检（控制台）
    └── modbus_slave_sim.py  Modbus TCP 从站模拟器
```

依赖方向：`UI → Industrial.Core`，`Core` 不引用任何 UI。

## 构建

```powershell
cd 项目
dotnet build src\Industrial.Core\Industrial.Core.csproj
dotnet build src\SerialAssistant\SerialAssistant.csproj
dotnet build src\ModbusMonitor\ModbusMonitor.csproj
dotnet build src\ScadaDashboard\ScadaDashboard.csproj
dotnet build src\Exercises\Exercises.ConsoleApp\Exercises.ConsoleApp.csproj
dotnet build src\Exercises\Exercises.WinForms\Exercises.WinForms.csproj
dotnet build src\Exercises\Exercises.Wpf\Exercises.Wpf.csproj
dotnet build tools\SelfCheck\SelfCheck.csproj
```

一次构建整个解决方案：

```powershell
dotnet build UpperComputer.Learning.slnx -p:UseSharedCompilation=false
```

> 在受限沙箱/CI 里，MSBuild 共享编译进程可能连不上命名管道，表现为“生成失败，0 个错误”。
> 加 `-p:UseSharedCompilation=false` 或逐项目构建即可。

## 运行

```powershell
python tools\modbus_slave_sim.py            # 终端 1（默认 502）
dotnet run --project tools\SelfCheck -- 502 # 自检 + 联调
dotnet run --project src\ModbusMonitor
dotnet run --project src\ScadaDashboard
dotnet run --project src\SerialAssistant
```

## 课后练习答案

教程第 01、02、04、05、07、10 章的 33 道练习，集中在本目录 `src\Exercises\` 下：

```powershell
dotnet run --project src\Exercises\Exercises.ConsoleApp   # 02 / 07 / 10 章
dotnet run --project src\Exercises\Exercises.WinForms     # 01 / 04 章
dotnet run --project src\Exercises\Exercises.Wpf          # 05 章
```

题目与代码逐条对照见 [`src/Exercises/README.md`](src/Exercises/README.md)。

## 技术要点

- 目标框架：`net8.0-windows`（WinForms/WPF）、`net8.0`（Core、SelfCheck）
- 零 NuGet 依赖：串口用 Win32 API，图表用 GDI+/DrawingContext 手写
- 配置：`src/ModbusMonitor/config/appsettings.json`
- 数据：`bin/.../data/yyyyMMdd.csv`，日志：`bin/.../logs/`

## 建议的阅读顺序

1. `Industrial.Core/Modbus/Crc16.cs` → `RegisterCodec.cs` → `ModbusTcpMaster.cs` → `ModbusRtuMaster.cs`
2. `Industrial.Core/Services/PollingService.cs`（批量读、取消、故障降级）
3. `SerialAssistant/MainForm.cs`（后台接收 + UI 更新）
4. `ModbusMonitor/MainForm.cs` + `TrendChart.cs`
5. `ScadaDashboard/**`（MVVM）

## 免责

仅用于学习。控制真实设备前必须做安全评估、联锁与权限控制。
