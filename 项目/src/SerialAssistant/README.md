# 项目 1：串口调试助手（WinForms）

上位机工程师的“万用表”。用来验证串口参数、收发 HEX、观察设备回包。

## 功能

- 串口参数：端口（自动枚举）、波特率、数据位、校验、停止位
- 打开失败给出中文原因（占用 / 不存在 / 驱动异常）
- 接收：HEX / 文本（UTF-8、GB2312、ASCII）、时间戳、自动滚动、暂停、清空
- 发送：HEX / 文本、追加 `\r\n`、Ctrl+Enter、定时发送（50ms~10min）
- 统计：收/发字节数、状态栏提示
- 保存接收内容为 txt

## 运行

```powershell
# 在 `项目` 目录下执行
# 终端 1：先启动免驱动虚拟串口对工具，窗口里点「① 启动服务」
dotnet run --project ..\com0com虚拟串口工具\VComPair

# 终端 2：串口调试助手
dotnet run --project src\SerialAssistant
```

> 从仓库根目录运行时：`dotnet run --project 项目/src/SerialAssistant`。

打开窗口后，端口下拉框选 **VCOM1**，点「打开串口」，然后在 VComPair 工具的
**VCOM2** 面板里点「发送」，本窗口接收区就会收到数据，反向也一样。
也可以直接用真实串口 / com0com 创建的 COM5 ↔ COM6，端口名照常填即可。

完整教程：

- [`../../../文档/17-项目一串口调试助手完全教程.md`](../../../文档/17-项目一串口调试助手完全教程.md)（本项目的逐段讲解）
- [`../../../com0com虚拟串口工具/README.md`](../../../com0com虚拟串口工具/README.md)（虚拟串口 / com0com 全套教程）

## 关键文件

| 文件 | 说明 |
|---|---|
| `Program.cs` | 入口、全局异常、注册 GB2312 编码 |
| `MainForm.Designer.cs` | 全部布局（FlowLayoutPanel + SplitContainer） |
| `MainForm.cs` | 串口生命周期、后台接收、HEX 解析、定时发送；VCOM 口与物理口在这里分流 |
| `../Industrial.Core/Transport/SerialPortNative.cs` | Win32 物理串口实现（CreateFile/SetCommState/ReadFile） |
| `../Industrial.Core/Transport/VirtualSerialPort.cs` | 免驱动虚拟串口实现（TCP 回环连 VComPair） |
| `../Industrial.Core/Transport/ISerialPort.cs` | 两者共用的抽象，业务代码只认接口 |

## 练习

- [ ] 发送历史（最近 10 条，持久化）
- [ ] 自动应答（收到指定帧回固定帧）
- [ ] 帧解析器（按 `AA 55` 帧头切帧）
- [ ] 统计 CRC 错误次数、帧间隔
- [ ] 保存/加载串口参数
