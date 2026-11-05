# 项目 2：Modbus 采集监控上位机（WinForms）

本仓库的核心项目：配置驱动的多设备采集、实时表格、趋势曲线、报警、历史存储、写寄存器。

## 功能

- JSON 配置设备（TCP/RTU）、站号、点位、数据类型、字序、量程、报警限
- 采集引擎：后台线程、地址合并批量读（≤125 寄存器/帧）、失败标 Bad、自动重连
- 实时表格：值、单位、时间、质量、报警，按状态染色
- 趋势图：手写 GDI+ 曲线、报警上下限、自动量程
- 报警记录：超限/恢复各一条，带时间与数值
- 历史：按天写 CSV（Excel 可直接打开），自动清理旧文件
- 写入：右键点位 → 输入工程值 → 06/10 功能码下发

## 运行

```powershell
# 终端 1：模拟器
python tools\modbus_slave_sim.py

# 终端 2：上位机
dotnet run --project src\ModbusMonitor
```

点“▶ 启动采集”→ 数值跳动；点某行 → 曲线切换；点“📁 数据目录”看 CSV。

## 配置

`config/appsettings.json`（输出目录也会复制一份）：

```json
{
  "Devices": [{
    "Name": "1#锅炉PLC",
    "Protocol": "ModbusTcp",
    "Host": "127.0.0.1",
    "Port": 502,
    "SlaveId": 1,
    "PollIntervalMs": 500,
    "Points": [
      { "Name": "炉膛温度", "Address": 0, "DataType": "Int16", "Scale": 0.1, "Unit": "℃", "HighLimit": 950 }
    ]
  }]
}
```

## 关键文件

| 文件 | 说明 |
|---|---|
| `MainForm.cs` | 配置、采集启停、报警、写值、CSV 落盘 |
| `TrendChart.cs` | GDI+ 双缓冲曲线 |
| `ViewModels.cs` | `TagRow`/`AlarmRecord` 数据绑定 |
| `InputDialog.cs` | 写值输入框 |
| `../Industrial.Core/Services/PollingService.cs` | 采集引擎 |
| `../Industrial.Core/Modbus/*` | 协议实现 |

## 扩展任务

- [ ] CSV 换 SQLite，增加历史查询页
- [ ] 报警确认/复位 + 操作人
- [ ] 用户登录与权限
- [ ] 配方下发与读回校验
- [ ] 班报/日报导出 Excel
- [ ] 断线缓存与补传
- [ ] 多设备并行 + 设备树

## 注意

本程序默认写 `data/` 与 `logs/`，请确保运行目录有写权限。生产使用请先做安全评估。
