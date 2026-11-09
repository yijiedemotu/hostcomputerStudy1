# 上位机学习大全：从 0 到找工作

> 一套**文档 + 可运行项目 + 离线联调工具**的完整教程，面向想入行「工业上位机软件」的零基础或转行同学。
> 主线技术栈：**C# / .NET 8 / WinForms + WPF / Modbus / SQL**，全部代码零第三方依赖，克隆下来就能编译运行。

---

## 1. 你会得到什么

| 内容 | 说明 |
|---|---|
| **24 篇文档** | 岗位地图、环境、C#、工程化、WinForms、WPF、通信、Modbus、PLC/OPC UA、数据库、多线程、架构、部署、现场调试、项目实战、求职、12 周计划、4 个附录，外加三个项目的独立完整教程（17/18/19） |
| **3 个可运行项目** | 串口调试助手、Modbus 采集监控上位机、WPF 监控看板 |
| **1 套虚拟串口方案** | `com0com虚拟串口工具/`：免驱动 VComPair 工具（零管理员权限）+ com0com 官方驱动从下载、装驱动、建端口对到排错的完整教程 |
| **1 套手敲实战教程** | `com0com虚拟串口工具/从零手敲教程/`：9 章渐进式，从 VS 2022 建项目开始逐行敲出 VComPair，每个里程碑都能编译运行 |
| **33 道课后练习答案** | 第 01/02/04/05/07/10 章练习，独立放在 `练习题答案/` 解决方案（Console、WinForms、WPF 三个可运行项目） |
| **1 个协议自检工具** | CRC / 字序 / RTU 组帧 / TCP 联调 / 采集引擎，5 组自动化测试 |
| **1 个 Modbus 从站模拟器** | Python 标准库实现，没有 PLC 也能完整联调 |
| **求职材料** | 简历模板、项目描述范例、100 道面试题速答 + 背诵题库、12 周打卡表 |

---

## 2. 30 秒快速开始

需要：Windows 10/11、[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)、Python 3（仅模拟器需要）。

```powershell
cd 项目

# ① 虚拟串口对工具（新开一个终端，保持运行；窗口里点「① 启动服务」）
dotnet run --project ..\com0com虚拟串口工具\VComPair

# ② 启动 Modbus 从站模拟器（再新开一个终端，保持运行）
python tools\modbus_slave_sim.py

# ③ 协议自检：CRC、字序、RTU、TCP、采集引擎
dotnet run --project tools\SelfCheck -- 502

# ④ 串口调试助手：端口选 VCOM1 → 打开串口 → 和工具的 VCOM2 面板互发
dotnet run --project src\SerialAssistant

# ⑤ Modbus 采集监控上位机（点「▶ 启动采集」）
dotnet run --project src\ModbusMonitor

# ⑥ WPF 监控看板
dotnet run --project src\ScadaDashboard
```

预期：模拟器打印监听信息；SelfCheck 全部 ✔；串口助手能完整收发；上位机表格数值跳动、曲线开始绘制。

> 502 端口被占用时：`python tools\modbus_slave_sim.py --port 1502`，
> 然后把 `src/ModbusMonitor/config/appsettings.json` 和 WPF 项目里的端口改成 1502。

---

## 3. 目录结构

```
上位机学习大全/
├── README.md                      ← 你在这里
├── 学习资源清单.md
├── 文档/
│   ├── 00-上位机岗位地图与学习路线.md  ~  16-12周学习计划与打卡表.md
│   ├── 17-项目一串口调试助手完全教程.md        ← 新增
│   ├── 18-项目二Modbus采集监控完全教程.md      ← 新增
│   ├── 19-项目三WPF监控看板完全教程.md         ← 新增
│   └── 附录A ~ 附录D
├── 项目/
│   ├── UpperComputer.Learning.slnx
│   ├── README.md
│   ├── src/
│   │   ├── Industrial.Core/       协议栈 + 模型 + 存储 + 日志（三个项目共用的类库）
│   │   ├── SerialAssistant/       项目 1：串口调试助手（WinForms）
│   │   ├── ModbusMonitor/         项目 2：采集监控上位机（WinForms）
│   │   └── ScadaDashboard/        项目 3：监控看板（WPF + MVVM）
│   └── tools/
│       ├── SelfCheck/             协议自检（控制台）
│       └── modbus_slave_sim.py    Modbus TCP 从站模拟器
├── 练习题答案/                    课后练习答案（独立解决方案，不要和主项目混在一起）
│   ├── 练习题答案.sln / .slnx
│   └── src/Answers.Console / Answers.WinForms / Answers.Wpf
└── com0com虚拟串口工具/           ← 新增：虚拟串口工具 + 完整教程
    ├── README.md                 导航与 5 分钟上手
    ├── 01 ~ 06 号教程            com0com 原理 / 安装 / 建端口对 / 联调 / 排错 / 免驱动工具
    ├── 从零手敲教程/             ← 9 章手敲实战：VS 2022 / 界面 / 代码 / 交互 / 排错
    ├── 一键启动工具.bat
    └── VComPair/                 免驱动虚拟串口对工具源码（C# / WinForms）
```

---

## 4. 三个项目分别练什么

| 项目 | 技术点 | 适合阶段 | 运行 |
|---|---|---|---|
| 串口调试助手 | 串口生命周期、后台接收、HEX/文本、定时发送、跨线程 UI | 第 3~4 周 | `dotnet run --project src\SerialAssistant` |
| Modbus 监控上位机 | Modbus RTU/TCP、批量读、趋势图、报警、CSV 历史、写寄存器 | 第 6~8 周 | `dotnet run --project src\ModbusMonitor` |
| WPF 看板 | XAML、绑定、ICommand、DataTemplate、OnRender 自绘 | 第 9 周 | `dotnet run --project src\ScadaDashboard` |

每个项目目录下都有独立 `README.md`，写明了功能、关键文件、扩展任务。
另外为三个项目各补了一篇**完整教程**（从运行、界面操作、代码逐段讲解到排错与扩展）：

- [`文档/17-项目一串口调试助手完全教程.md`](文档/17-项目一串口调试助手完全教程.md)
- [`文档/18-项目二Modbus采集监控完全教程.md`](文档/18-项目二Modbus采集监控完全教程.md)
- [`文档/19-项目三WPF监控看板完全教程.md`](文档/19-项目三WPF监控看板完全教程.md)

总览性质的讲解见 `文档/14-项目实战三个项目讲解.md`。

---

## 5. 学习路线（详细版见文档 00 / 16）

```
第 1~2 周   C# 基础 + 工程化
第 3~4 周   WinForms + 串口 → 项目 1
第 5~7 周   网络/协议 + Modbus + 采集 → 项目 2
第 8 周     数据库 + 历史查询
第 9 周     WPF + MVVM → 项目 3
第 10~11 周 PLC/OPC UA + 日志部署 + 打包
第 12 周    简历 + 面试 + 投递
```

每天 2 小时、每周 6 天。时间紧可用文档 16 的 6 周压缩版。

---

## 6. 已验证的内容

本仓库交付前实际执行过：

- ✅ 3 个主项目 + 共用类库 + SelfCheck 全部编译通过（`dotnet build`，0 错误）
- ✅ 独立解决方案 `练习题答案/` 中 33 道课后练习答案编译通过；Console 全题实跑，WinForms / WPF 界面逐页启动验证
- ✅ `SelfCheck` 离线测试通过：CRC16 标准值 `CDC5`、四种字序往返、RTU 内存回环读/写/线圈
- ✅ 免驱动虚拟串口端到端实测：VCOM1 ↔ VCOM2 双向收发、超时返回 0、端口独占、回环模式、离线缓存补发、中文标签全部通过
- ✅ 启动 Python 模拟器后 TCP 联调通过：读站号 1/2、写设定值回读、非法地址返回异常码 `02`
- ✅ 启动 Python 模拟器后 TCP 联调通过：读站号 1/2、写设定值回读、非法地址返回异常码 `02`
- ✅ 采集引擎端到端：3 秒内多个快照、质量 Good、断线时上报 Bad 且不崩溃
- ⚠️ GUI 窗口需要在**交互式桌面**中运行；练习答案界面已实际启动并截图验证，完整项目界面请在本机 `dotnet run` 查看

> 复现自检：
> ```powershell
> cd 项目
> python tools\modbus_slave_sim.py     # 终端 1
> dotnet run --project tools\SelfCheck -- 502
> ```

---

## 7. 环境要求

| 项 | 要求 |
|---|---|
| 系统 | Windows 10/11 64 位（上位机岗位几乎只用 Windows） |
| SDK | .NET 8 SDK（含 WindowsDesktop 运行时） |
| 编辑器 | Visual Studio 2022（.NET 桌面开发）或 VS Code + C# 扩展 |
| Python | 3.9+，仅模拟器需要（无第三方包） |
| 硬件 | 无。虚拟串口用本仓库自带的 VComPair 工具即可；真机联调时准备 USB 转 RS485 |

本项目**刻意不使用 NuGet 包**（本机离线环境无法还原），物理串口用 Win32 API 实现、免驱动虚拟串口用 TCP 回环、图表用 GDI+/DrawingContext 手写。
这不是“最佳工程实践”，但能让你**在任何一台没网的工控机上编译通过**，也更能看懂底层。

---

## 8. 如何使用这套材料

### 零基础
从 `文档/00` 顺序读到 `16`，每章做完验收任务；项目按 1 → 2 → 3 顺序做。

### 有编程基础、没做过上位机
直接看 `06`、`07`、`10`、`14`，配合项目 2 的源码；再补 `04`/`05` 的界面部分。

### 面试冲刺
看 `15` + `附录C`，把项目 2 的架构、批量读、故障降级、字序讲熟；再用 `附录D` 逐题背诵，跑一遍 `SelfCheck` 当演示。

---

## 9. 常见问题

**Q：`dotnet build` 报“生成失败，0 个错误”？**
在受限沙箱/CI 中，MSBuild 的共享编译进程可能连不上命名管道。逐项目构建即可：
`dotnet build src\Industrial.Core\Industrial.Core.csproj -p:UseSharedCompilation=false`

**Q：没有串口，串口助手能测吗？**
能，而且不需要装 com0com。先跑 `dotnet run --project com0com虚拟串口工具/VComPair`，在窗口点「① 启动服务」；
再打开串口助手，端口下拉框选 **VCOM1** 点「打开串口」，用工具里的 VCOM2 面板互发即可。
完整步骤见 [`com0com虚拟串口工具/README.md`](com0com虚拟串口工具/README.md)。
如果用的是第三方、只认 `COM5/COM6` 这类名字的调试软件，才需要真正的内核驱动 com0com，见同一文件夹的 02 / 03 号教程。

**Q：为什么不用 NModbus / LiveCharts？**
学习阶段手写一遍协议和图形，面试时能讲清原理；生产项目该用库就用库，文档里给了选型建议。

**Q：项目 2 的数据存在哪？**
`src/ModbusMonitor/bin/Debug/net8.0-windows/data/yyyyMMdd.csv`，点工具栏“📁 数据目录”直接打开。

**Q：模拟器能用于生产吗？**
不能。它只是学习用的测试桩，没有并发保护、安全策略和完整异常处理。

---

## 10. 免责声明

- 本项目仅用于学习与技术验证，**未经加固不要直接用于生产设备控制**。
- 涉及真实 PLC/设备的写操作请在联锁、急停、权限完备的前提下进行。
- 文档中的薪资、招聘要求为行业普遍情况的概括，不构成任何承诺。

---

**下一步**：打开 [`文档/00-上位机岗位地图与学习路线.md`](文档/00-上位机岗位地图与学习路线.md)，或者直接跑第 2 节的命令把项目跑起来。
