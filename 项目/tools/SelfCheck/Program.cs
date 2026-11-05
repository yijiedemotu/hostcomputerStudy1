using System.Text;
using Industrial.Core.Modbus;
using Industrial.Core.Transport;

namespace SelfCheck;

/// <summary>
/// 协议自检工具：不开界面、不需要硬件，验证 CRC、字节序、RTU 组帧、TCP 收发。
///
/// 这是“写上位机必须有的第二套验证手段”：
///   界面能跑不代表协议对；协议对不对要用已知报文和回环测试证明。
///
/// 用法：
///   dotnet run --project tools/SelfCheck                 # 只跑离线测试
///   python tools/modbus_slave_sim.py                     # 另开一个窗口启动模拟器
///   dotnet run --project tools/SelfCheck -- 502          # 再跑含 TCP 联调的测试
/// </summary>
internal static class Program
{
    internal static int Failures;

    private static int Main(string[] args)
    {
        try { Console.OutputEncoding = Encoding.UTF8; } catch { }

        Console.WriteLine("========================================");
        Console.WriteLine(" 上位机学习项目 · 协议自检 SelfCheck");
        Console.WriteLine("========================================\n");

        TestCrc();
        TestRegisterCodec();
        TestRtuLoopback();
        TestTcp(args);
        PollingTests.Run();

        Console.WriteLine();
        Console.WriteLine(Failures == 0 ? "全部通过 ✔" : $"有 {Failures} 项失败 ✘");
        return Failures == 0 ? 0 : 1;
    }

    // ---------------- 1. CRC ----------------

    private static void TestCrc()
    {
        Console.WriteLine("[1] CRC16 校验");

        var request = new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x0A };
        var crc = Crc16.Compute(request);
        Check("01 03 00 00 00 0A 的 CRC = CDC5", crc == 0xCDC5, $"实际 {crc:X4}");

        var frame = new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x0A, 0xC5, 0xCD };
        Check("整帧 CRC 校验通过", Crc16.Check(frame));

        frame[7] ^= 0xFF;
        Check("篡改后 CRC 校验失败", !Crc16.Check(frame));

        var appended = Crc16.Append(request);
        Check("Append 生成低字节在前的 CRC", appended[^2] == 0xC5 && appended[^1] == 0xCD);
        Console.WriteLine();
    }

    // ---------------- 2. 数据类型与字序 ----------------

    private static void TestRegisterCodec()
    {
        Console.WriteLine("[2] 寄存器编解码 / 字序");

        var abcd = RegisterCodec.Encode(0x12345678, RegisterDataType.UInt32, RegisterOrder.Abcd);
        Check("ABCD 0x12345678 → 1234 5678", abcd[0] == 0x1234 && abcd[1] == 0x5678,
            $"实际 {abcd[0]:X4} {abcd[1]:X4}");

        var cdab = RegisterCodec.Encode(0x12345678, RegisterDataType.UInt32, RegisterOrder.Cdab);
        Check("CDAB 0x12345678 → 5678 1234", cdab[0] == 0x5678 && cdab[1] == 0x1234,
            $"实际 {cdab[0]:X4} {cdab[1]:X4}");

        var badc = RegisterCodec.Encode(0x12345678, RegisterDataType.UInt32, RegisterOrder.Badc);
        Check("BADC 0x12345678 → 3412 7856", badc[0] == 0x3412 && badc[1] == 0x7856,
            $"实际 {badc[0]:X4} {badc[1]:X4}");

        var dcba = RegisterCodec.Encode(0x12345678, RegisterDataType.UInt32, RegisterOrder.Dcba);
        Check("DCBA 0x12345678 → 7856 3412", dcba[0] == 0x7856 && dcba[1] == 0x3412,
            $"实际 {dcba[0]:X4} {dcba[1]:X4}");

        foreach (var order in Enum.GetValues<RegisterOrder>())
        {
            var encoded = RegisterCodec.Encode(123.456, RegisterDataType.Float32, order);
            var decoded = RegisterCodec.Decode(encoded, RegisterDataType.Float32, order);
            Check($"Float32 往返（{order}）", Math.Abs(decoded - 123.456) < 0.001, $"得到 {decoded}");

            var intEncoded = RegisterCodec.Encode(-123456, RegisterDataType.Int32, order);
            var intDecoded = RegisterCodec.Decode(intEncoded, RegisterDataType.Int32, order);
            Check($"Int32 往返（{order}）", (int)intDecoded == -123456, $"得到 {intDecoded}");
        }

        var scaled = RegisterCodec.ToEngineering(865, 0.1, 0);
        Check("工程量换算 865 × 0.1 = 86.5", Math.Abs(scaled - 86.5) < 1e-9);
        Check("反算 86.5 → 865", Math.Abs(RegisterCodec.ToRaw(86.5, 0.1, 0) - 865) < 1e-9);
        Console.WriteLine();
    }

    // ---------------- 3. RTU 组帧（内存回环从站） ----------------

    private static void TestRtuLoopback()
    {
        Console.WriteLine("[3] Modbus RTU 组帧（内存回环）");

        var settings = new SerialSettings { PortName = "LOOP", BaudRate = 9600 };
        using var port = new LoopbackRtuSlave();
        using var master = new ModbusRtuMaster(port, settings, timeoutMs: 800);

        try
        {
            master.ConnectAsync().GetAwaiter().GetResult();
            var values = master.ReadHoldingRegistersAsync(7, 0, 3).GetAwaiter().GetResult();
            Check("读保持寄存器（站号 7，3 个）",
                values.Length == 3 && values[0] == 100 && values[1] == 200 && values[2] == 300,
                string.Join(",", values));

            master.WriteSingleRegisterAsync(7, 0, 1234).GetAwaiter().GetResult();
            var afterWrite = master.ReadHoldingRegistersAsync(7, 0, 1).GetAwaiter().GetResult();
            Check("写单寄存器后回读 = 1234", afterWrite[0] == 1234, $"实际 {afterWrite[0]}");

            var coils = master.ReadCoilsAsync(7, 0, 4).GetAwaiter().GetResult();
            Check("读线圈返回 4 位", coils.Length == 4);
        }
        catch (Exception ex)
        {
            Failures++;
            Console.WriteLine($"  ✘ RTU 回环测试异常：{ex.GetType().Name}: {ex.Message}");
        }
        Console.WriteLine();
    }

    // ---------------- 4. TCP 联调（需要模拟器） ----------------

    private static void TestTcp(string[] args)
    {
        Console.WriteLine("[4] Modbus TCP 联调（需要先启动模拟器）");

        var port = 502;
        if (args.Length > 0 && int.TryParse(args[0], out var parsed)) port = parsed;

        using var master = new ModbusTcpMaster("127.0.0.1", port, timeoutMs: 2000);
        try
        {
            master.ConnectAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  - 跳过：连不上 127.0.0.1:{port}（{ex.Message}）");
            Console.WriteLine("    提示：另开一个终端运行  python tools/modbus_slave_sim.py");
            Console.WriteLine("          然后执行          dotnet run --project tools/SelfCheck -- 502");
            Console.WriteLine();
            return;
        }

        Console.WriteLine($"  已连接 127.0.0.1:{port}");

        try
        {
            var unit1 = master.ReadHoldingRegistersAsync(1, 0, 5).GetAwaiter().GetResult();
            Console.WriteLine($"  站号1 原始寄存器：{string.Join(" ", unit1.Select(v => $"{v}(0x{v:X4})"))}");
            Console.WriteLine($"    模温={unit1[0] * 0.1:0.0}℃  压力={unit1[1] * 0.01:0.00}MPa  周期={unit1[2] * 0.1:0.0}s");

            var production = (uint)((unit1[3] << 16) | unit1[4]);
            Console.WriteLine($"    累计产量(ABCD)={production} 件");
            Check("站号1 温度在合理范围 0~200℃", unit1[0] * 0.1 is > 0 and < 200);

            master.WriteSingleRegisterAsync(1, 5, 888).GetAwaiter().GetResult();
            var setpoint = master.ReadHoldingRegistersAsync(1, 5, 1).GetAwaiter().GetResult();
            Check("写设定值 888 后回读一致", setpoint[0] == 888, $"实际 {setpoint[0]}");

            var unit2 = master.ReadHoldingRegistersAsync(2, 0, 2).GetAwaiter().GetResult();
            Console.WriteLine($"  站号2：压力={unit2[0] * 0.01:0.00}MPa 温度={unit2[1] * 0.1:0.0}℃");
            Check("站号2 可独立读取", unit2.Length == 2);

            try
            {
                master.ReadHoldingRegistersAsync(1, 9000, 1).GetAwaiter().GetResult();
                Failures++;
                Console.WriteLine("  ✘ 非法地址本应返回异常码 0x02，但读成功了");
            }
            catch (ModbusException ex)
            {
                Check("非法地址返回异常码 02", ex.ExceptionCode == 0x02, $"实际 {ex.ExceptionCode:X2}");
            }
        }
        catch (Exception ex)
        {
            Failures++;
            Console.WriteLine($"  ✘ TCP 测试异常：{ex.GetType().Name}: {ex.Message}");
        }
        Console.WriteLine();
    }

    private static void Check(string name, bool condition, string? detail = null)
    {
        if (condition)
        {
            Console.WriteLine($"  ✔ {name}");
        }
        else
        {
            Failures++;
            Console.WriteLine($"  ✘ {name}{(detail is null ? "" : " —— " + detail)}");
        }
    }

    /// <summary>
    /// 内存里的 RTU 从站：实现 ISerialPort，把“写出去”的请求解析后生成应答放进接收队列。
    /// 这样一个假串口就能完整验证主站的组帧、CRC、超时与解析逻辑，不需要任何硬件。
    /// </summary>
    private sealed class LoopbackRtuSlave : ISerialPort
    {
        private readonly Queue<byte> _rx = new();
        private readonly ushort[] _registers = new ushort[64];
        private readonly bool[] _coils = new bool[64];

        public string PortName => "LOOP";
        public bool IsOpen { get; private set; }

        public void Open()
        {
            IsOpen = true;
            for (var i = 0; i < 16; i++) _registers[i] = (ushort)(100 * (i + 1));
            _coils[0] = true;
        }

        public void Close() => IsOpen = false;

        public void DiscardBuffers()
        {
            lock (_rx) _rx.Clear();
        }

        public void Write(byte[] data)
        {
            if (data.Length < 4 || !Crc16.Check(data)) return;

            var unit = data[0];
            var function = data[1];
            byte[] response;

            if (function is 0x03 or 0x04 && data.Length >= 8)
            {
                var start = (ushort)((data[2] << 8) | data[3]);
                var count = (ushort)((data[4] << 8) | data[5]);
                var pdu = new List<byte> { unit, function, (byte)(count * 2) };
                for (var i = 0; i < count; i++)
                {
                    var value = start + i < _registers.Length ? _registers[start + i] : (ushort)0;
                    pdu.Add((byte)(value >> 8));
                    pdu.Add((byte)(value & 0xFF));
                }
                response = Crc16.Append(pdu.ToArray());
            }
            else if (function == 0x06 && data.Length >= 8)
            {
                var address = (ushort)((data[2] << 8) | data[3]);
                var value = (ushort)((data[4] << 8) | data[5]);
                if (address >= _registers.Length)
                {
                    response = Crc16.Append(new byte[] { unit, (byte)(function | 0x80), 0x02 });
                }
                else
                {
                    _registers[address] = value;
                    response = Crc16.Append(data[..6]);
                }
            }
            else if (function is 0x01 or 0x02 && data.Length >= 8)
            {
                var start = (ushort)((data[2] << 8) | data[3]);
                var count = (ushort)((data[4] << 8) | data[5]);
                var byteCount = (count + 7) / 8;
                var payload = new byte[byteCount];
                for (var i = 0; i < count; i++)
                    if (start + i < _coils.Length && _coils[start + i])
                        payload[i / 8] |= (byte)(1 << (i % 8));
                var pdu = new List<byte> { unit, function, (byte)byteCount };
                pdu.AddRange(payload);
                response = Crc16.Append(pdu.ToArray());
            }
            else
            {
                response = Crc16.Append(new byte[] { unit, (byte)(function | 0x80), 0x01 });
            }

            lock (_rx)
            {
                foreach (var b in response) _rx.Enqueue(b);
            }
        }

        public int Read(byte[] buffer, int offset, int count, int timeoutMs)
        {
            var deadline = Environment.TickCount64 + timeoutMs;
            while (true)
            {
                lock (_rx)
                {
                    if (_rx.Count > 0)
                    {
                        var n = Math.Min(count, _rx.Count);
                        for (var i = 0; i < n; i++) buffer[offset + i] = _rx.Dequeue();
                        return n;
                    }
                }

                if (Environment.TickCount64 > deadline) return 0;
                Thread.Sleep(1);
            }
        }

        public void Dispose() => Close();
    }
}
