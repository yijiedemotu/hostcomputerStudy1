using Industrial.Core.Models;
using Industrial.Core.Modbus;
using Industrial.Core.Transport;

namespace Industrial.Core.Services;

/// <summary>
/// 主站工厂：根据设备配置里的协议，创建对应的 Modbus 主站。
/// 上层只拿到 IModbusMaster 接口，不关心底层是 TCP 还是串口，
/// 这就是“简单工厂 + 面向接口”的组合。
/// </summary>
public static class ModbusMasterFactory
{
    /// <summary>
    /// 创建设备对应的主站。
    /// timeoutMs：单次请求超时；enableDtrRts：RTU 是否需要拉高 DTR/RTS（有些 485 转换器靠它供电/换向）。
    /// </summary>
    public static IModbusMaster Create(DeviceConfig device, int timeoutMs = 1000, bool enableDtrRts = true)
    {
        return device.Protocol switch
        {
            // TCP：只需要 IP + 端口
            DeviceProtocol.ModbusTcp => new ModbusTcpMaster(device.Host, device.Port, timeoutMs),

            // RTU：组装串口参数，创建 Win32 串口实现
            DeviceProtocol.ModbusRtu => CreateRtu(device, timeoutMs, enableDtrRts),

            _ => throw new NotSupportedException($"不支持的协议：{device.Protocol}")
        };
    }

    private static IModbusMaster CreateRtu(DeviceConfig device, int timeoutMs, bool enableDtrRts)
    {
        var settings = device.ToSerialSettings();

        // 这里用 Win32 API 自己封装的串口，而不是 System.IO.Ports，
        // 原因是 .NET Core 的 System.IO.Ports 在部分工控机上不稳定（需要额外 NuGet 包）。
        var port = new SerialPortNative(settings) { EnableDtrRts = enableDtrRts };
        return new ModbusRtuMaster(port, settings, timeoutMs);
    }
}
