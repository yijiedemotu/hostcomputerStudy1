# VComPair 源码说明

免驱动的虚拟串口对工具，**一个程序**同时提供：虚拟串口对服务端 + 两个内置终端 + 数据监控窗口。

- 目标框架：`net8.0-windows` / WinForms，**零 NuGet 依赖**
- 默认监听：`127.0.0.1:9527`
- 端点：`VCOM1` ↔ `VCOM2`
- 完整教程：[`../06-VComPair免驱动工具完全教程.md`](../06-VComPair免驱动工具完全教程.md)
- 手敲教程：[`../从零手敲教程/README.md`](../从零手敲教程/README.md)（9 章，从 VS 2022 建项目开始逐行实现本工具）

## 编译与运行

```powershell
cd D:\上位机\上位机学习大全
dotnet build com0com虚拟串口工具/VComPair/VComPair.csproj
dotnet run   --project com0com虚拟串口工具/VComPair
```

或者双击上级目录的 `一键启动工具.bat`。

运行后点窗口里的「① 启动服务」，再启动串口调试助手并选 `VCOM1`。
界面预览见上级目录 `VComPair-界面预览.png`（服务已启动、发出一帧后的实际截图）。

> 界面布局用 **Dock + SplitContainer**，没有用嵌套 TableLayoutPanel：
> 后者若忘记给单列容器设 `ColumnStyles(Percent 100)`，列宽会按 AutoSize 塌缩，控件全挤到左上角。
> 另注意两点：Fill 控件要第一个 Add；`SplitContainer` 的 `Panel1MinSize/Panel2MinSize`
> 不要在窗体构造函数里设置（此时控件无实际尺寸，会抛 `InvalidOperationException`），
> 分栏位置在 `Shown` 事件里设置。

## 文件清单

| 文件 | 职责 |
|---|---|
| `Program.cs` | 入口，`[STAThread]` + `ApplicationConfiguration.Initialize()` |
| `MainForm.cs` | 主窗口：工具条、两个终端面板、监控、状态栏；启停 PairHub |
| `TerminalPanel.cs` | 端点面板：连接/断开、HEX/文本收发、自动应答、接收区限流 |
| `PairHub.cs` | 服务端核心：监听、HELLO 握手、端点独占、双向转发、离线缓存、回环、事件 |
| `VirtualClient.cs` | 内置终端用的客户端，与 `VirtualSerialPort` 同协议 |
| `ProtocolHelper.cs` | HEX 解析 / HEX / ASCII 显示 |

## 协议速览

```
客户端 → 服务端： HELLO|VCOM1|SerialAssistant\n     (UTF-8 一行)
服务端 → 客户端： +OK|VCOM1\n   或  -ERR|原因\n
之后：            原始字节透明转发，不分帧、不转义
```

## 与仓库的对应关系

| 位置 | 作用 |
|---|---|
| `项目/src/Industrial.Core/Transport/ISerialPort.cs` | 物理口与虚拟口共用的接口 |
| `项目/src/Industrial.Core/Transport/SerialPortNative.cs` | 物理 COM 口实现（Win32 API） |
| `项目/src/Industrial.Core/Transport/VirtualSerialPort.cs` | 虚拟口实现（连本工具） |
| `项目/src/SerialAssistant/MainForm.cs` | 端口列表追加 VCOM1/VCOM2，按名字选择实现 |

## 设计取舍

- **用 TCP 回环而不是造 COM 设备**：Windows 上真正的 COM 口必须由内核驱动注册设备对象，纯托管代码做不到；那是 com0com 的工作。
- **只监听 127.0.0.1**：学习/联调场景不需要跨机，也避免暴露端口。
- **握手用 UTF-8**：中文错误提示和"内置终端"标签不会变成 `?`。
- **`Read` 用 `DataAvailable` 轮询**：保证"超时返回 0、不抛异常"的语义与 `SerialPortNative.Read` 一致，业务层的接收循环不用改。

## 注意事项

- 关闭窗口会停止服务、断开所有连接，不残留后台进程或虚拟设备。
- `VCOM1/VCOM2` 不会出现在设备管理器；第三方只认 `COMx` 的软件请改用 com0com（见上级 `02` / `03` 号教程）。
- 本工具仅用于学习与联调，不是生产设备通讯组件。
