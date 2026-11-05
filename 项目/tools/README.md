# tools：没有硬件也能联调

## modbus_slave_sim.py

纯 Python 标准库的 Modbus TCP 从站，模拟两台设备：

| 站号 | 设备 | 寄存器 |
|---|---|---|
| 1 | 1#注塑机 | 0 模温(Int16,0.1℃)、1 注射压力(UInt16,0.01MPa)、2 周期(0.1s)、3~4 累计产量(UInt32,ABCD)、5 设定值(可写) |
| 2 | 2#空压机 | 0 排气压力(UInt16,0.01MPa)、1 排气温度(Int16,0.1℃) |

支持功能码：01/02/03/04/05/06/10；异常应答 01/02/0B。

```powershell
python modbus_slave_sim.py                 # 0.0.0.0:502
python modbus_slave_sim.py --port 1502     # 换端口
python modbus_slave_sim.py --verbose       # 打印每一帧
```

数值每 0.5 秒按正弦变化，产量每 5 秒 +1，方便观察曲线和报警。

## SelfCheck

```powershell
dotnet run --project tools/SelfCheck            # 离线：CRC / 字序 / RTU 回环
dotnet run --project tools/SelfCheck -- 502     # 加 TCP 联调与采集引擎测试
```

5 组测试：

1. CRC16（标准帧 `01 03 00 00 00 0A` → `CDC5`）
2. 寄存器编解码与四种字序
3. RTU 组帧（内存假串口，无需硬件）
4. TCP 联调（读、写、异常码）
5. 采集引擎（快照、质量、故障降级）

退出码 0 表示全部通过。

## 虚拟串口（可选）

串口助手没有硬件时，可安装 com0com 创建一对虚拟串口（COM5 ↔ COM6），
一端发、另一端收，用来验证 HEX 收发和定时发送。
