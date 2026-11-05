namespace Industrial.Core.Modbus;

/// <summary>
/// Modbus 主站（Master/Client）统一接口。
/// 现场设备无论走网口（Modbus TCP）还是 485（Modbus RTU），
/// 上层采集逻辑都只认这个接口——这是“面向接口编程”在工控里最典型的好处：
/// 换协议时只改工厂里 new 出来的实现，采集代码一行都不用动。
/// </summary>
public interface IModbusMaster : IDisposable
{
    /// <summary>当前是否已建立连接。</summary>
    bool IsConnected { get; }

    /// <summary>连接目标，例如 "192.168.1.10:502" 或 "COM3@9600"，用于日志显示。</summary>
    string Endpoint { get; }

    /// <summary>建立连接。TCP 是连服务器，RTU 是打开串口。</summary>
    Task ConnectAsync(CancellationToken ct = default);

    /// <summary>主动断开连接，释放底层资源。</summary>
    void Disconnect();

    // 以下是 Modbus 的 8 个常用读写方法。
    // unitId：从站号（站地址）；startAddress：0 基址的寄存器地址；count：读取数量。
    // 所有方法都支持 CancellationToken，窗口关闭或用户点“停止”时可以立刻取消。

    /// <summary>读线圈（功能码 01），每路是一位开关量。</summary>
    Task<bool[]> ReadCoilsAsync(byte unitId, ushort startAddress, ushort count, CancellationToken ct = default);

    /// <summary>读离散输入（功能码 02），只读的开关量。</summary>
    Task<bool[]> ReadDiscreteInputsAsync(byte unitId, ushort startAddress, ushort count, CancellationToken ct = default);

    /// <summary>读保持寄存器（功能码 03），最常用，可读可写。</summary>
    Task<ushort[]> ReadHoldingRegistersAsync(byte unitId, ushort startAddress, ushort count, CancellationToken ct = default);

    /// <summary>读输入寄存器（功能码 04），通常只读，仪表测量值常放这里。</summary>
    Task<ushort[]> ReadInputRegistersAsync(byte unitId, ushort startAddress, ushort count, CancellationToken ct = default);

    /// <summary>写单个线圈（功能码 05）。</summary>
    Task WriteSingleCoilAsync(byte unitId, ushort address, bool value, CancellationToken ct = default);

    /// <summary>写单个保持寄存器（功能码 06）。</summary>
    Task WriteSingleRegisterAsync(byte unitId, ushort address, ushort value, CancellationToken ct = default);

    /// <summary>连续写多个保持寄存器（功能码 10 / 0x10），写 32 位数据时用。</summary>
    Task WriteMultipleRegistersAsync(byte unitId, ushort startAddress, ushort[] values, CancellationToken ct = default);
}
