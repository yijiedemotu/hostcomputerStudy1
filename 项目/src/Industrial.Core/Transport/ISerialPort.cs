namespace Industrial.Core.Transport;

/// <summary>
/// 串口抽象。
///
/// 为什么要抽一层接口？
/// 1. 业务代码（Modbus RTU、串口调试助手）不依赖具体实现，将来换成 System.IO.Ports
///    或 USB 转 485 的厂商 DLL，只需换实现；
/// 2. 单元测试可以塞一个假实现（内存管道），不用真硬件；
/// 3. 本仓库为了“离线零依赖”提供了一个 Win32 版实现
///    <see cref="SerialPortNative"/>，官方 SerialPort 类的用法见文档《07 通信基础》。
///
/// 读取采用“带超时的阻塞读”，调用方自己在后台线程里循环读，
/// 这样比事件回调更好控制粘包/半包，也更容易写超时逻辑。
/// </summary>
public interface ISerialPort : IDisposable
{
    /// <summary>串口当前是否已打开。</summary>
    bool IsOpen { get; }

    /// <summary>串口名，例如 "COM3"。</summary>
    string PortName { get; }

    /// <summary>打开串口。参数不匹配或端口被占用时会抛异常。</summary>
    void Open();

    /// <summary>关闭串口，释放系统资源。</summary>
    void Close();

    /// <summary>发送数据（内部会处理部分写）。</summary>
    void Write(byte[] data);

    /// <summary>
    /// 最多读 <paramref name="count"/> 个字节。
    /// 返回实际读到的字节数；超时无数据返回 0（不是异常，便于循环）。
    /// </summary>
    int Read(byte[] buffer, int offset, int count, int timeoutMs);

    /// <summary>清空发送/接收缓冲区。Modbus 请求前清一下可避免上一帧的残包。</summary>
    void DiscardBuffers();
}
