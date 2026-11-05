using Answers.ConsoleApp.Modbus;

namespace Answers.ConsoleApp;

/// <summary>
/// 第 07 章 · 练习 6：Modbus 主站连续失败 3 次后自动重连。
///
/// 题目：模拟网络抖动，让前 3 次读取失败；封装层在连续失败达到阈值时断开重连，
/// 并重试当前这一次读取。业务代码只负责“读数据”，不需要写重连逻辑。
///
/// 设计要点：
/// 1. 使用“装饰器模式”：ReconnectingMaster 和真实主站实现同一个 IModbusMaster 接口，
///    业务代码拿到的还是“一个主站”，感觉不到重连细节；
/// 2. 连续失败才重连，避免一次抖动就频繁断开；
/// 3. 重连后只重试当前请求一次，避免无限重试把线程卡死；
/// 4. 用 SemaphoreSlim 保证同一时刻只有一个请求在操作连接，防止多线程把连接搞乱。
/// </summary>
internal static class Exercise06_Reconnect
{
    public static async Task RunAsync()
    {
        ConsoleUi.Section("练习 6：Modbus 主站失败 3 次自动重连");

        // using 保证练习结束后调用 Dispose，释放连接和信号量。
        // 工厂 lambda 每次需要新主站时创建一个“前 3 次读失败”的假从站。
        using var master = new ReconnectingMaster(
            () => new FlakyTcpMaster(failuresBeforeSuccess: 3),
            failureThreshold: 3,
            log: message => Console.WriteLine("    " + message));

        ConsoleUi.Note("模拟前 3 次读失败（网络抖动），第 3 次失败后封装层强制断开并重连，然后重试本次读取。");
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            try
            {
                // 业务代码看起来和普通主站完全一样，不知道内部会重连。
                var values = await master.ReadHoldingRegistersAsync(1, 0, 3);
                ConsoleUi.KeyValue($"第 {attempt} 次业务调用", $"成功，读到 {string.Join(", ", values)}");
                ConsoleUi.Answer("重连策略应封装在主站装饰器/连接层，业务代码只负责读数据；不要在每个按钮事件里写重连。");
                return;
            }
            catch (Exception ex)
            {
                ConsoleUi.KeyValue($"第 {attempt} 次业务调用", $"失败：{ex.Message}");
            }

            // 两次业务调用之间稍等，模拟上位机界面的轮询节奏。
            await Task.Delay(150);
        }

        ConsoleUi.Error("5 次调用仍未成功，检查重连策略。");
    }

    /// <summary>
    /// 假从站：前 N 次读取故意抛 IOException，模拟网络抖动；
    /// 之后返回固定的 100、200、300，表示链路恢复。
    /// </summary>
    private sealed class FlakyTcpMaster(int failuresBeforeSuccess) : IModbusMaster
    {
        private int _reads;
        private int _connects;

        public bool IsConnected { get; private set; }
        public string Endpoint => "192.168.1.10:502";

        public Task ConnectAsync(CancellationToken ct = default)
        {
            _connects++;
            IsConnected = true;
            Console.WriteLine($"    [从站] 建立连接 #{_connects}");
            return Task.CompletedTask;
        }

        public void Disconnect()
        {
            if (!IsConnected) return;
            IsConnected = false;
            Console.WriteLine("    [从站] 连接已断开");
        }

        public Task<ushort[]> ReadHoldingRegistersAsync(byte unitId, ushort startAddress, ushort count, CancellationToken ct = default)
        {
            if (!IsConnected) throw new IOException("连接未建立");

            _reads++;
            // 前 failuresBeforeSuccess 次读取都失败，模拟“网络还没恢复”。
            if (_reads <= failuresBeforeSuccess)
                throw new IOException($"模拟网络抖动（第 {_reads} 次读）");

            return Task.FromResult(new ushort[] { 100, 200, 300 });
        }

        // 本题只演示读保持寄存器，其他方法不支持。
        public Task<bool[]> ReadCoilsAsync(byte unitId, ushort startAddress, ushort count, CancellationToken ct = default)
            => throw new NotSupportedException();
        public Task<bool[]> ReadDiscreteInputsAsync(byte unitId, ushort startAddress, ushort count, CancellationToken ct = default)
            => throw new NotSupportedException();
        public Task<ushort[]> ReadInputRegistersAsync(byte unitId, ushort startAddress, ushort count, CancellationToken ct = default)
            => throw new NotSupportedException();
        public Task WriteSingleCoilAsync(byte unitId, ushort address, bool value, CancellationToken ct = default)
            => throw new NotSupportedException();
        public Task WriteSingleRegisterAsync(byte unitId, ushort address, ushort value, CancellationToken ct = default)
            => throw new NotSupportedException();
        public Task WriteMultipleRegistersAsync(byte unitId, ushort startAddress, ushort[] values, CancellationToken ct = default)
            => throw new NotSupportedException();
        public void Dispose() => Disconnect();
    }

    /// <summary>
    /// 重连装饰器：包住一个真实主站，对外仍然表现为 IModbusMaster。
    ///
    /// 所有读写都走 ExecuteAsync：
    /// - 成功：清零失败计数，直接返回结果；
    /// - 失败：失败计数 +1；达到阈值时断开、等待 300ms、重连，并重试一次；
    /// - 重连后仍失败：把异常抛给业务代码，由业务决定报警/降级。
    /// </summary>
    private sealed class ReconnectingMaster : IModbusMaster
    {
        private readonly Func<IModbusMaster> _factory;
        private readonly int _threshold;
        private readonly Action<string> _log;

        // SemaphoreSlim(1,1) 相当于一把“只允许一个人进”的锁，
        // 但它是异步锁：等待时用 await，不会阻塞线程。
        private readonly SemaphoreSlim _gate = new(1, 1);

        private IModbusMaster _inner;
        private int _consecutiveFailures;
        private bool _disposed;

        public ReconnectingMaster(Func<IModbusMaster> factory, int failureThreshold, Action<string>? log = null)
        {
            // 阈值至少为 1，否则逻辑没有意义。
            if (failureThreshold < 1) throw new ArgumentOutOfRangeException(nameof(failureThreshold));

            _factory = factory;
            _threshold = failureThreshold;
            _log = log ?? (_ => { });   // 没传日志方法就用一个空方法，避免后面判空
            _inner = factory();
        }

        public bool IsConnected => _inner.IsConnected;
        public string Endpoint => _inner.Endpoint;

        public Task ConnectAsync(CancellationToken ct = default)
            => ExecuteAsync(async m =>
            {
                await m.ConnectAsync(ct);
                return true;    // ExecuteAsync 需要返回值，连接操作返回一个占位的 true
            }, ct);

        public void Disconnect() => _inner.Disconnect();

        public Task<bool[]> ReadCoilsAsync(byte unitId, ushort startAddress, ushort count, CancellationToken ct = default)
            => ExecuteAsync(m => m.ReadCoilsAsync(unitId, startAddress, count, ct), ct);

        public Task<bool[]> ReadDiscreteInputsAsync(byte unitId, ushort startAddress, ushort count, CancellationToken ct = default)
            => ExecuteAsync(m => m.ReadDiscreteInputsAsync(unitId, startAddress, count, ct), ct);

        public Task<ushort[]> ReadHoldingRegistersAsync(byte unitId, ushort startAddress, ushort count, CancellationToken ct = default)
            => ExecuteAsync(m => m.ReadHoldingRegistersAsync(unitId, startAddress, count, ct), ct);

        public Task<ushort[]> ReadInputRegistersAsync(byte unitId, ushort startAddress, ushort count, CancellationToken ct = default)
            => ExecuteAsync(m => m.ReadInputRegistersAsync(unitId, startAddress, count, ct), ct);

        public async Task WriteSingleCoilAsync(byte unitId, ushort address, bool value, CancellationToken ct = default)
            => await ExecuteAsync(async m =>
            {
                await m.WriteSingleCoilAsync(unitId, address, value, ct);
                return true;
            }, ct);

        public async Task WriteSingleRegisterAsync(byte unitId, ushort address, ushort value, CancellationToken ct = default)
            => await ExecuteAsync(async m =>
            {
                await m.WriteSingleRegisterAsync(unitId, address, value, ct);
                return true;
            }, ct);

        public async Task WriteMultipleRegistersAsync(byte unitId, ushort startAddress, ushort[] values, CancellationToken ct = default)
            => await ExecuteAsync(async m =>
            {
                await m.WriteMultipleRegistersAsync(unitId, startAddress, values, ct);
                return true;
            }, ct);

        /// <summary>
        /// 统一执行“连接检查 + 操作 + 失败统计 + 按需重连重试”。
        /// 泛型 T 让读线圈（bool[]）、读寄存器（ushort[]）、写操作（bool）都能复用。
        /// </summary>
        private async Task<T> ExecuteAsync<T>(Func<IModbusMaster, Task<T>> operation, CancellationToken ct)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            // 进入临界区。若另一个请求正在执行，这里会异步等待，不占线程。
            await _gate.WaitAsync(ct);
            try
            {
                var retriedAfterReconnect = false;

                while (true)
                {
                    try
                    {
                        // 如果尚未连接，先连接。
                        if (!_inner.IsConnected)
                            await _inner.ConnectAsync(ct);

                        // 真正执行读/写。
                        var result = await operation(_inner);

                        // 之前失败过、这次成功：记录“恢复”，并把连续失败清零。
                        if (_consecutiveFailures > 0)
                        {
                            _log($"[恢复] 成功，失败计数清零（之前连续失败 {_consecutiveFailures} 次）");
                            _consecutiveFailures = 0;
                        }
                        return result;
                    }
                    catch (OperationCanceledException)
                    {
                        // 用户主动取消（例如关闭程序），不算通信失败，直接向上抛。
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _consecutiveFailures++;
                        _log($"[失败] 连续第 {_consecutiveFailures} 次：{ex.Message}");

                        // 达到阈值，且本次调用还没重试过 → 断开、重连、重试当前请求。
                        if (_consecutiveFailures >= _threshold && !retriedAfterReconnect)
                        {
                            retriedAfterReconnect = true;
                            _log($"[重连] 连续失败达到 {_threshold} 次，回收连接并重连...");

                            // 旧连接可能已经坏了，Disconnect 本身也可能抛异常，这里忽略。
                            try { _inner.Disconnect(); } catch { }

                            // 等 300ms 再连，给网络/从站一点恢复时间（现场可按需调整）。
                            await Task.Delay(300, ct);

                            try
                            {
                                await _inner.ConnectAsync(ct);
                                _log("[重连] 成功，重试当前请求");
                            }
                            catch (Exception reconnectEx)
                            {
                                _log($"[重连] 失败：{reconnectEx.Message}");
                                throw;   // 重连都失败，只能把异常交给业务层
                            }

                            // continue 回到 while 顶部，再执行一次当前操作。
                            continue;
                        }

                        // 未达到阈值，或已经重试过一次仍失败 → 抛给调用方。
                        throw;
                    }
                }
            }
            finally
            {
                // 无论成功、失败还是异常，都必须释放锁，否则后面的请求永远进不来。
                _gate.Release();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _inner.Dispose();
            _gate.Dispose();
        }
    }
}
