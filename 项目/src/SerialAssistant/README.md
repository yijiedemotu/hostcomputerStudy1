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
dotnet run --project src\SerialAssistant
```

无串口也能打开界面；收发需要真实串口或 com0com 虚拟串口对。

## 关键文件

| 文件 | 说明 |
|---|---|
| `Program.cs` | 入口、全局异常、注册 GB2312 编码 |
| `MainForm.Designer.cs` | 全部布局（FlowLayoutPanel + SplitContainer） |
| `MainForm.cs` | 串口生命周期、后台接收、HEX 解析、定时发送 |
| `../Industrial.Core/Transport/SerialPortNative.cs` | Win32 串口实现（CreateFile/SetCommState/ReadFile） |

## 练习

- [ ] 发送历史（最近 10 条，持久化）
- [ ] 自动应答（收到指定帧回固定帧）
- [ ] 帧解析器（按 `AA 55` 帧头切帧）
- [ ] 统计 CRC 错误次数、帧间隔
- [ ] 保存/加载串口参数
