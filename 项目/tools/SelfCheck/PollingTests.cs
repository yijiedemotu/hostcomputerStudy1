using Industrial.Core.Models;
using Industrial.Core.Modbus;
using Industrial.Core.Services;

namespace SelfCheck;

/// <summary>[5] 采集引擎端到端：启动 PollingService，等 3 秒，检查快照与质量。</summary>
internal static class PollingTests
{
    public static void Run()
    {
        Console.WriteLine("[5] 采集引擎端到端（需要模拟器）");

        if (!TcpReachable(502))
        {
            Console.WriteLine("  - 跳过：127.0.0.1:502 未监听");
            Console.WriteLine();
            return;
        }

        var config = new AppConfig
        {
            Devices =
            {
                new DeviceConfig
                {
                    Name = "自检设备",
                    Protocol = DeviceProtocol.ModbusTcp,
                    Host = "127.0.0.1",
                    Port = 502,
                    SlaveId = 1,
                    PollIntervalMs = 300,
                    Points =
                    {
                        new PointConfig { Name = "温度", Address = 0, DataType = RegisterDataType.Int16, Scale = 0.1, Unit = "℃" },
                        new PointConfig { Name = "压力", Address = 1, DataType = RegisterDataType.UInt16, Scale = 0.01, Unit = "MPa" }
                    }
                }
            }
        };

        var snapshots = 0;
        var goodValues = 0;
        var lastTemperature = 0.0;

        var service = new PollingService(config);
        service.SnapshotReady += list =>
        {
            Interlocked.Increment(ref snapshots);
            foreach (var value in list)
            {
                if (value.Quality != Quality.Good) continue;
                Interlocked.Increment(ref goodValues);
                if (value.TagName == "温度") lastTemperature = value.Value;
            }
        };

        service.Start();
        Thread.Sleep(3000);
        service.StopAsync().Wait(5000);

        Report("3 秒内收到多个快照", snapshots >= 3, $"实际 {snapshots} 个");
        Report("采集到质量良好的数据", goodValues > 0, $"实际 {goodValues} 个");
        Report("温度在 0~200℃ 之间", lastTemperature is > 0 and < 200, $"实际 {lastTemperature}℃");

        // 故障路径：连一个没人监听的端口，采集引擎应把数据标成 Bad，而不是抛异常退出。
        var broken = new AppConfig
        {
            Devices =
            {
                new DeviceConfig
                {
                    Name = "离线设备",
                    Protocol = DeviceProtocol.ModbusTcp,
                    Host = "127.0.0.1",
                    Port = 1,
                    SlaveId = 1,
                    PollIntervalMs = 300,
                    Points = { new PointConfig { Name = "温度", Address = 0, DataType = RegisterDataType.Int16 } }
                }
            }
        };

        var badReports = 0;
        var brokenService = new PollingService(broken);
        brokenService.SnapshotReady += list =>
        {
            if (list.Any(v => v.Quality == Quality.Bad)) Interlocked.Increment(ref badReports);
        };

        brokenService.Start();
        Thread.Sleep(2000);
        brokenService.StopAsync().Wait(5000);

        Report("通信失败时上报 Bad 质量（不崩溃）", badReports > 0, $"实际 {badReports} 次");
        Console.WriteLine();
    }

    private static bool TcpReachable(int port)
    {
        try
        {
            using var client = new System.Net.Sockets.TcpClient();
            return client.ConnectAsync("127.0.0.1", port).Wait(1500) && client.Connected;
        }
        catch
        {
            return false;
        }
    }

    private static void Report(string name, bool ok, string? detail = null)
    {
        if (ok)
        {
            Console.WriteLine($"  ✔ {name}");
        }
        else
        {
            Console.WriteLine($"  ✘ {name}{(detail is null ? "" : " —— " + detail)}");
            Program.Failures++;
        }
    }
}
