// 说明：本文件是为了让「串口助手跟敲作业」这个独立项目能够自行编译而补上的接口定义。
// 在完整仓库里，它位于 项目/src/Industrial.Core/Transport/ISerialPort.cs，
// VirtualSerialPort.cs 与 SerialPortNative.cs 都实现这个接口。
// 作业项目没有引用 Industrial.Core 类库，所以在这里补一份同样的抽象。

namespace Industrial.Core.Transport;

/// <summary>
/// 串口抽象：业务代码只依赖这个接口，换实现（物理 COM 口 / 虚拟 VCOM 口 / 内存假串口）不用改业务。
/// 读取采用"带超时的阻塞读"，超时返回 0 而不是抛异常，便于上层循环。
/// </summary>
public interface ISerialPort : IDisposable
{
    /// <summary>串口当前是否已打开。</summary>
    bool IsOpen { get; }

    /// <summary>串口名，例如 "COM3" 或 "VCOM1"。</summary>
    string PortName { get; }

    /// <summary>打开串口。参数不匹配或端口被占用时会抛异常。</summary>
    void Open();

    /// <summary>关闭串口，释放系统资源。</summary>
    void Close();

    /// <summary>发送数据（内部会处理部分写）。</summary>
    void Write(byte[] data);

    /// <summary>
    /// 最多读 count 个字节。返回实际读到的字节数；超时无数据返回 0（不是异常）。
    /// </summary>
    int Read(byte[] buffer, int offset, int count, int timeoutMs);

    /// <summary>清空发送/接收缓冲区。</summary>
    void DiscardBuffers();
}
