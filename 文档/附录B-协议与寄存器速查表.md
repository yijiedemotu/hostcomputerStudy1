# 附录 B 协议与寄存器速查表

---

## B1. Modbus 功能码

| 码 | 名称 | 读/写 | 数量上限 | 请求数据 | 应答数据 |
|---|---|---|---|---|---|
| 01 | Read Coils | 读 | 2000 位 | 起始地址 2 + 数量 2 | 字节数 1 + 位数据 |
| 02 | Read Discrete Inputs | 读 | 2000 位 | 同上 | 同上 |
| 03 | Read Holding Registers | 读 | 125 个 | 起始地址 2 + 数量 2 | 字节数 1 + 数据 |
| 04 | Read Input Registers | 读 | 125 个 | 同上 | 同上 |
| 05 | Write Single Coil | 写 | 1 位 | 地址 2 + FF00/0000 | 原样回显 |
| 06 | Write Single Register | 写 | 1 个 | 地址 2 + 值 2 | 原样回显 |
| 0F | Write Multiple Coils | 写 | 1968 位 | 地址 2 + 数量 2 + 字节数 1 + 数据 | 地址 2 + 数量 2 |
| 10 | Write Multiple Registers | 写 | 123 个 | 地址 2 + 数量 2 + 字节数 1 + 数据 | 地址 2 + 数量 2 |

---

## B2. Modbus 异常码

| 码 | 名称 | 常见原因 |
|---|---|---|
| 01 | Illegal Function | 从站不支持该功能码 |
| 02 | Illegal Data Address | 地址越界、基址差 1、数量超限 |
| 03 | Illegal Data Value | 写入值超范围、字节数不符 |
| 04 | Slave Device Failure | 从站内部故障 |
| 05 | Acknowledge | 已接受，处理中 |
| 06 | Slave Device Busy | 从站忙，稍后重试 |
| 08 | Memory Parity Error | 存储校验错 |
| 0A | Gateway Path Unavailable | 网关配置错误 |
| 0B | Gateway Target Failed to Respond | 站号错、从站掉线 |

异常帧格式：`[站号][功能码|0x80][异常码][CRC]`

---

## B3. Modbus 地址模型

| 区 | 5 位地址 | 协议地址 | 读写 | 数据 |
|---|---|---|---|---|
| 线圈 | 00001~09999 | 0~9998 | 读写 | 位 |
| 离散输入 | 10001~19999 | 0~9998 | 只读 | 位 |
| 输入寄存器 | 30001~39999 | 0~9998 | 只读 | 16 位 |
| 保持寄存器 | 40001~49999 | 0~9998 | 读写 | 16 位 |

换算：

```
协议地址 = 手册地址 - 区起始（40001/30001/10001/00001）
例：40001 → 0；40101 → 100；30001 → 0
```

> 有些设备用 1 基址（40001 → 1），对不上时试 0/1 两种偏移。

---

## B4. 字节序

| 名称 | 寄存器 1 | 寄存器 2 | 0x12345678 |
|---|---|---|---|
| ABCD | 0x1234 | 0x5678 | 高字在前，字内大端 |
| CDAB | 0x5678 | 0x1234 | 低字在前，字内大端 |
| BADC | 0x3412 | 0x7856 | 高字在前，字内小端 |
| DCBA | 0x7856 | 0x3412 | 低字在前，字内小端 |

---

## B5. 串口

| 项 | 说明 |
|---|---|
| 常用参数 | 9600, 8, N, 1；19200, 8, E, 1 |
| DB9（RS232） | 2=RXD，3=TXD，5=GND，7=RTS，8=CTS |
| RS485 | A/D+，B/D-，屏蔽层单端接地 |
| 终端电阻 | 总线两端各 120Ω |
| 最大距离 | 9600 约 1200m；115200 约 100m |
| 常见芯片 | CH340、CP2102、FT232、PL2303 |
| 驱动问题 | 设备管理器出现“未知设备”/黄色感叹号 |

---

## B6. 波特率与距离（RS485，经验值）

| 波特率 | 最大距离 |
|---|---|
| 1200 | 1200 m |
| 2400 | 1200 m |
| 4800 | 1200 m |
| 9600 | 1200 m |
| 19200 | 1000 m |
| 38400 | 800 m |
| 57600 | 500 m |
| 115200 | 100~200 m |

---

## B7. 常用端口

| 协议 | 端口 |
|---|---|
| Modbus TCP | 502 |
| Modbus RTU over TCP | 502（常见） |
| 西门子 S7 | 102 |
| OPC UA | 4840 |
| OPC DA | 135（DCOM） |
| EtherNet/IP | 44818 |
| MQTT | 1883 / 8883(TLS) |
| HTTP | 80 / 443 |
| 三菱 MC | 5000/6000（可配） |
| 欧姆龙 FINS | 9600 |

---

## B8. 常见 PLC 地址写法

| 品牌 | 地址示例 | 说明 |
|---|---|---|
| 西门子 | `DB1.DBW0`、`M0.0`、`I0.0`、`Q0.0` | DB 块需关闭“优化的块访问” |
| 三菱 | `D100`、`M100`、`W1A0` | MC 协议 |
| 欧姆龙 | `D100`、`CIO0.01` | FINS |
| 台达 | `D100`、`M100` | Modbus 映射 |
| 汇川 | `D100`、`H0'` | 多为 Modbus |
| 仪表 | `40001`、`00001` | Modbus 手册地址 |

---

## B9. 单位与换算

| 物理量 | 常用单位 | 说明 |
|---|---|---|
| 温度 | ℃ | 常见 Scale 0.1 |
| 压力 | MPa / kPa / bar | 1 MPa = 10 bar ≈ 10.2 kgf/cm² |
| 流量 | m³/h、L/min、t/h | 注意标况/工况 |
| 液位 | m、mm | 常见 0.001 |
| 重量 | kg、g、t | 称重仪表常见 0.01 |
| 频率 | Hz | 变频器 |
| 转速 | rpm | 编码器 |
| 电量 | kWh、A、V | 电表常见 0.01 |
| 时间 | s、ms | 周期、超时 |

---

## B10. 常用 NuGet 包

| 包 | 用途 |
|---|---|
| `System.IO.Ports` | 串口 |
| `Microsoft.Data.Sqlite` | SQLite |
| `Microsoft.EntityFrameworkCore.Sqlite` | EF Core SQLite |
| `NModbus` | Modbus（学习阶段建议手写） |
| `S7netplus` | 西门子 S7 |
| `OPCFoundation.NetStandard.Opc.Ua` | OPC UA |
| `Serilog` / `NLog` | 日志 |
| `CommunityToolkit.Mvvm` | WPF MVVM |
| `ClosedXML` / `NPOI` | Excel |
| `LiveChartsCore` / `ScottPlot` | 图表 |
| `HslCommunication` | 多品牌 PLC（商业授权注意） |
| `AutoUpdater.NET` | 自动更新 |

---

## B11. 调试工具

| 工具 | 用途 |
|---|---|
| 串口调试助手 | 串口收发（本仓库项目 1） |
| NetAssist / TCP&UDP 调试助手 | 网络收发 |
| Wireshark | 抓包 |
| Modbus Poll / Slave | Modbus 主/从模拟 |
| UaExpert | OPC UA 客户端 |
| com0com | 内核级虚拟串口对（COM5↔COM6），教程：`com0com虚拟串口工具/02`、`03` |
| VComPair | 本仓库免驱动虚拟串口对（VCOM1↔VCOM2），教程：`com0com虚拟串口工具/06` |
| Process Explorer | 进程/线程/句柄 |
| `dotnet-counters` | .NET 运行时指标 |
| `dotnet-dump` | 崩溃/死锁转储 |

---

## B12. 常见错误码（Win32）

| 码 | 含义 | 常见场景 |
|---|---|---|
| 2 | 文件不存在 | 串口不存在 |
| 5 | 拒绝访问 | 串口被占用、权限不足 |
| 31 | 设备未连接 | USB 转串口拔出 |
| 87 | 参数错误 | 波特率/数据位非法 |
| 1167 | 设备无法识别 | 驱动异常 |
| 10048 | 端口已被使用 | TCP 端口冲突 |
| 10060 | 连接超时 | 网络不通/防火墙 |
| 10061 | 连接被拒绝 | 服务未启动 |
