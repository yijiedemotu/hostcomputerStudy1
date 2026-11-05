using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace Industrial.Core.Transport;

/// <summary>
/// 纯 Win32 API 实现的串口（不依赖 System.IO.Ports 包），仅 Windows。
///
/// 这是一个“教学用的备选实现”：官方推荐用 System.IO.Ports.SerialPort，
/// 但本机离线环境拿不到该 NuGet 包，于是直接调 kernel32：
/// CreateFile 打开 \\.\COMx → GetCommState/SetCommState 配参数
/// → SetCommTimeouts 设超时 → ReadFile/WriteFile 收发 → CloseHandle 关闭。
///
/// 面试常问：“串口打不开有哪些原因？” 见文档《14 现场调试手册》，
/// 这里把每个失败点都翻译成中文提示。
/// </summary>
public sealed class SerialPortNative : ISerialPort
{
    // 以下常量对应 Win32 API 的标志位，数值来自 Windows SDK 头文件
    private const uint GenericRead = 0x80000000;
    private const uint GenericWrite = 0x40000000;
    private const uint OpenExisting = 3;         // CreateFile：只打开已存在的设备
    private const uint FileAttributeNormal = 0x80;
    private const uint PurgeTxClear = 0x0004;    // 清发送缓冲区
    private const uint PurgeRxClear = 0x0008;    // 清接收缓冲区

    private readonly SerialSettings _settings;

    // SafeFileHandle 是 Win32 句柄的安全包装：Dispose 时自动 CloseHandle，避免句柄泄漏
    private SafeFileHandle? _handle;

    public SerialPortNative(SerialSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public string PortName => _settings.PortName;

    // 句柄存在、且既未失效也未关闭，才算打开
    public bool IsOpen => _handle is { IsInvalid: false, IsClosed: false };

    /// <summary>是否让 DTR/RTS 保持有效。部分 USB-RS485 转换器靠 RTS 控制收发方向。</summary>
    public bool EnableDtrRts { get; set; } = true;

    public void Open()
    {
        if (IsOpen) return;
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("本实现仅支持 Windows。");

        // COM10 及以上必须加 \\.\ 前缀，否则 CreateFile 会当成文件名。
        var path = _settings.PortName.StartsWith(@"\\.\", StringComparison.Ordinal)
            ? _settings.PortName
            : @"\\.\" + _settings.PortName;

        var handle = CreateFileW(path, GenericRead | GenericWrite, 0, IntPtr.Zero,
            OpenExisting, FileAttributeNormal, IntPtr.Zero);

        if (handle.IsInvalid)
        {
            var err = Marshal.GetLastWin32Error();
            handle.Dispose();
            throw new IOException($"{_settings.PortName} 打开失败：{DescribeError(err)}（Win32 {err}）");
        }

        _handle = handle;
        try
        {
            // 设置驱动内部收发缓冲区大小（输入、输出各 4KB）
            SetupComm(handle, 4096, 4096);

            // DCB（Device Control Block）保存串口参数，先读当前值再修改
            var dcb = new Dcb { DCBlength = (uint)Marshal.SizeOf<Dcb>() };
            if (!GetCommState(handle, ref dcb))
                throw new IOException($"读取 {_settings.PortName} 参数失败（Win32 {Marshal.GetLastWin32Error()}）");

            dcb.BaudRate = (uint)_settings.BaudRate;
            dcb.ByteSize = (byte)_settings.DataBits;
            dcb.Parity = (byte)_settings.Parity;
            dcb.StopBits = (byte)_settings.StopBits;

            // fBinary=1（二进制模式）；有校验则 fParity=1；DTR/RTS 使能；其余握手流控全部关闭。
            uint flags = 0x0001;
            if (_settings.Parity != ParityMode.None) flags |= 0x0002;
            if (EnableDtrRts)
            {
                flags |= 1u << 4;  // fDtrControl = DTR_CONTROL_ENABLE
                flags |= 1u << 12; // fRtsControl = RTS_CONTROL_ENABLE
            }
            dcb.Flags = flags;

            if (!SetCommState(handle, ref dcb))
                throw new IOException($"设置 {_settings.PortName} 参数失败，检查波特率是否被设备支持（Win32 {Marshal.GetLastWin32Error()}）");

            // 参数设置完清一次缓冲，扔掉配置过程中产生的杂数据
            PurgeComm(handle, PurgeRxClear | PurgeTxClear);
        }
        catch
        {
            // 任何一步失败都要把句柄还回去，否则端口会一直被占用
            _handle = null;
            handle.Dispose();
            throw;
        }
    }

    public void Close()
    {
        var h = _handle;
        _handle = null;
        if (h is null) return;
        try { PurgeComm(h, PurgeRxClear | PurgeTxClear); } catch { /* 设备已拔出时忽略 */ }
        h.Dispose();
    }

    /// <summary>发送数据。WriteFile 可能只写一部分，所以要循环直到全部写完。</summary>
    public void Write(byte[] data)
    {
        var h = _handle;
        if (h is null || h.IsInvalid) throw new InvalidOperationException("串口未打开。");

        var offset = 0;
        while (offset < data.Length)
        {
            var remain = data.Length - offset;

            // P/Invoke 的 byte[] 参数不方便直接指定偏移，先复制出剩余部分
            var chunk = new byte[remain];
            Buffer.BlockCopy(data, offset, chunk, 0, remain);

            if (!WriteFile(h, chunk, (uint)remain, out var written, IntPtr.Zero) || written == 0)
                throw new IOException($"串口写失败（Win32 {Marshal.GetLastWin32Error()}）");

            offset += (int)written;
        }
    }

    /// <summary>
    /// 读数据。最多等 timeoutMs 毫秒，没数据返回 0（不抛异常）。
    /// 读超时参数由 Windows 的 COMMTIMEOUTS 结构控制。
    /// </summary>
    public int Read(byte[] buffer, int offset, int count, int timeoutMs)
    {
        var h = _handle;
        if (h is null || h.IsInvalid) throw new InvalidOperationException("串口未打开。");
        if (count <= 0) return 0;

        var timeouts = new CommTimeouts
        {
            // 两字节间隔超过 30ms 就认为一帧结束；总超时由 timeoutMs 控制。
            ReadIntervalTimeout = 30,
            ReadTotalTimeoutMultiplier = 0,
            ReadTotalTimeoutConstant = (uint)Math.Max(1, timeoutMs)
        };
        if (!SetCommTimeouts(h, ref timeouts))
            throw new IOException($"设置读超时失败（Win32 {Marshal.GetLastWin32Error()}）");

        var temp = new byte[count];
        if (!ReadFile(h, temp, (uint)count, out var read, IntPtr.Zero))
            throw new IOException($"串口读失败（Win32 {Marshal.GetLastWin32Error()}）");

        // 把读到的数据复制回调用方的缓冲区（从 offset 开始）
        var n = (int)read;
        if (n > 0) Buffer.BlockCopy(temp, 0, buffer, offset, n);
        return n;
    }

    public void DiscardBuffers()
    {
        var h = _handle;
        if (h is null || h.IsInvalid) return;
        PurgeComm(h, PurgeRxClear | PurgeTxClear);
    }

    public void Dispose() => Close();

    /// <summary>
    /// 枚举本机串口，例如 COM3、COM10（按数字排序）。
    /// 串口列表不在 .NET 里，而是 Windows 注册表 HARDWARE\DEVICEMAP\SERIALCOMM。
    /// </summary>
    public static string[] GetPortNames()
    {
        if (!OperatingSystem.IsWindows()) return [];
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DEVICEMAP\SERIALCOMM");
            if (key is null) return [];

            // 注册表值名是设备路径，值内容才是 COM 名，所以取 GetValue
            return key.GetValueNames()
                .Select(name => key.GetValue(name) as string)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v!)
                .Distinct()
                .OrderBy(PortNumber) // 按 COM 后面的数字排序，避免 COM10 排在 COM2 前面
                .ToArray();
        }
        catch
        {
            return [];
        }
    }

    /// <summary>从 "COM10" 里取出数字 10；解析失败排到最后。</summary>
    private static int PortNumber(string name) =>
        int.TryParse(name.AsSpan(3), out var n) ? n : int.MaxValue;

    /// <summary>把 Win32 错误码翻译成现场能看懂的排查提示。</summary>
    private static string DescribeError(int win32)
    {
        var msg = new Win32Exception(win32).Message;
        return win32 switch
        {
            2 or 3 => "端口不存在，检查 USB 转串口是否插好、驱动是否安装",
            5 => "端口被占用（通常是另一个调试软件/上位机还开着）",
            31 => "设备未连接或驱动异常",
            87 => "参数不合法（波特率/数据位写错）",
            1167 => "设备无法识别，换 USB 口或换线试试",
            _ => msg
        };
    }

    // ---------------- Win32 互操作 ----------------
    // 下面这些 DllImport 就是“调用 C 语言写的 Windows 系统函数”，
    // 参数类型和顺序必须和 Windows SDK 头文件完全一致，写错会崩溃或读错数据。

    /// <summary>
    /// DCB：Windows 串口配置结构，保存波特率、数据位、校验、停止位、流控等。
    /// StructLayout(Sequential) 表示字段按声明顺序紧密排列，和 C 结构体内存布局一致。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct Dcb
    {
        public uint DCBlength;
        public uint BaudRate;
        public uint Flags;
        public ushort wReserved;
        public ushort XonLim;
        public ushort XoffLim;
        public byte ByteSize;
        public byte Parity;
        public byte StopBits;
        public sbyte XonChar;
        public sbyte XoffChar;
        public sbyte ErrorChar;
        public sbyte EofChar;
        public sbyte EvtChar;
        public ushort wReserved1;
    }

    /// <summary>COMMTIMEOUTS：串口读写的超时规则（间隔超时、总超时等）。</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct CommTimeouts
    {
        public uint ReadIntervalTimeout;
        public uint ReadTotalTimeoutMultiplier;
        public uint ReadTotalTimeoutConstant;
        public uint WriteTotalTimeoutMultiplier;
        public uint WriteTotalTimeoutConstant;
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern SafeFileHandle CreateFileW(
        string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes,
        uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCommState(SafeFileHandle hFile, ref Dcb lpDcb);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetCommState(SafeFileHandle hFile, ref Dcb lpDcb);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetCommTimeouts(SafeFileHandle hFile, ref CommTimeouts lpCommTimeouts);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupComm(SafeFileHandle hFile, uint dwInQueue, uint dwOutQueue);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PurgeComm(SafeFileHandle hFile, uint dwFlags);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ReadFile(
        SafeFileHandle hFile, byte[] lpBuffer, uint nNumberOfBytesToRead,
        out uint lpNumberOfBytesRead, IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WriteFile(
        SafeFileHandle hFile, byte[] lpBuffer, uint nNumberOfBytesToWrite,
        out uint lpNumberOfBytesWritten, IntPtr lpOverlapped);
}
