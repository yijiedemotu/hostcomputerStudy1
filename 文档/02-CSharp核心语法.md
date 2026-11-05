# 02 C# 核心语法（上位机视角）

> 本章目标：用最短路径掌握“能干活”的 C#。不追求语言完备，只讲上位机天天用的部分。
> 每段都有可运行的小例子。**建议边看边在控制台项目里敲一遍。**

---

## 1. 类型：先分清“存什么”

```csharp
int count = 10;            // 32 位整数
ushort register = 0x03B1;  // 16 位无符号，Modbus 寄存器就是它
short temperature = -25;   // 16 位有符号，温度可能是负的
byte b = 0xA5;             // 8 位，串口字节
double pressure = 1.25;    // 双精度，工程值
decimal money = 12.34m;    // 十进制，金额/精确计算
bool running = true;
string name = "1#炉";
DateTime now = DateTime.Now;
```

选型经验：

| 场景 | 用什么 | 原因 |
|---|---|---|
| Modbus 寄存器 | `ushort` | 0~65535 |
| 串口字节 | `byte[]` | 底层就是字节流 |
| 温度/压力等工程量 | `double` | 够用、和浮点换算方便 |
| 金额、配方精确值 | `decimal` | 避免 0.1+0.2 != 0.3 |
| 32 位累计量 | `uint` / `int` | 注意溢出和字序 |

值类型（int、double、struct）复制的是值；引用类型（class、string、数组）复制的是引用。

```csharp
var a = new int[] { 1, 2 };
var b = a;
b[0] = 99;
Console.WriteLine(a[0]);   // 99 —— 同一个数组
```

---

## 2. 可空类型：区分“0”和“没数据”

```csharp
int? highLimit = null;       // 可能没配置上限
if (highLimit is { } high && value > high)
   //含义：**如果 highLimit 不为 null，就把它的值取出来放到变量 high，再判断 value 是否超过这个上限**
    Console.WriteLine("超上限");

double? maybe = null;
Console.WriteLine(maybe ?? 0);       // 空合并
Console.WriteLine(maybe.GetValueOrDefault());
```

**上位机里大量使用可空**：未配置的报警限、还没采到的值、可选的备注。
项目开启了 `<Nullable>enable</Nullable>`，编译器会提醒你哪里可能为 null——**别忽略这些警告**。

---

## 3. 字符串：现场问题的高发区

```csharp
string hex = Convert.ToHexString(new byte[] { 0x01, 0x03 });  // "0103"
byte[] data = Convert.FromHexString("0103");

ushort value = 0x03B1;
string s1 = value.ToString("X2");        // "3B1"？注意 X2 是最少 2 位，不截断
string s2 = value.ToString("X4");        // "03B1"
string s3 = 86.5.ToString("0.000");      // "86.500"
string s4 = 1234567.ToString("N0");      // "1,234,567"

// 解析：一定要用 TryParse，别让用户输入把程序搞崩
if (double.TryParse(input, out var v))
    Console.WriteLine(v);
else
    Console.WriteLine("输入不是数字");
```

✅ `"X"` 代表：**输出大写十六进制**（小写`x`就是小写字母 `3b1`） ✅ `Xn` 的含义：**最少 n 位，不足前面补 0；超过 n 位不截断、不丢数字**

### 3.1 区域性（Culture）坑

```csharp
double v = 1.5;
v.ToString();                                   // 德语环境可能变成 "1,5"
v.ToString(CultureInfo.InvariantCulture);       // 永远是 "1.5"
double.Parse("1.5", CultureInfo.InvariantCulture);
```

**写 CSV、配置文件、跟 PLC 交互时，一律用 `InvariantCulture`。**
本仓库的 `CsvHistoryStore` 就是这么做的。

### 3.2 字符串拼接

```csharp
// 少量拼接：插值字符串最清楚
string msg = $"[{DateTime.Now:HH:mm:ss}] 收到 {n} 字节";

// 循环里大量拼接：用 StringBuilder，别用 +=
var sb = new StringBuilder();
foreach (var b in data) sb.Append(b.ToString("X2")).Append(' ');
```

---

## 4. 集合：上位机数据的基本容器

```csharp
var list = new List<string> { "炉温", "压力" };
list.Add("流量");
list.Remove("压力");
list.Sort();

var map = new Dictionary<string, double>
{
    ["炉温"] = 865.0,
    ["压力"] = 1.25
};
if (map.TryGetValue("炉温", out var t)) Console.WriteLine(t);

var queue = new Queue<byte>();     // 接收缓冲区常用
queue.Enqueue(0x01);
var first = queue.Dequeue();

var set = new HashSet<string>();   // 去重
```

### LINQ：一行搞定筛选/排序/投影

```csharp
var points = device.Points
    .Where(p => p.Enabled)
    .OrderBy(p => p.Address)
    .Select(p => p.Name)
    .ToList();

var max = values.Max();
var average = values.Average();
var byDevice = values.GroupBy(v => v.DeviceName)
                     .ToDictionary(g => g.Key, g => g.Count());

var found = devices.FirstOrDefault(d => d.Name == "1#炉");   // 找不到返回 null
```

> **延迟执行**：LINQ 语句在 `ToList()`/`foreach` 时才真正执行。调试时如果发现“查了没变”，先想想是不是没触发枚举。

---

## 5. 方法与参数

```csharp
public static ushort ComputeCrc(ReadOnlySpan<byte> data, ushort init = 0xFFFF) { ... }

// ref 修改外部变量；out 输出；in 只读引用（大结构体省拷贝）
public static bool TryParseHex(string text, out byte[] result) { ... }
```

扩展方法（给已有类型加功能，本仓库 `Chunked()` 就是）：

```csharp
internal static class HexExtensions
{
    public static string ToHexString(this byte[] data) =>
        Convert.ToHexString(data);
}
```

---

## 6. 类、属性、记录

```csharp
public sealed class PointConfig
{
    public string Name { get; set; } = "";
    public ushort Address { get; set; }
    public double Scale { get; set; } = 1;
}

// record：值相等、适合做“一条数据”
public sealed record HistoryRecord(DateTime Time, string Tag, double Value);
```

属性写法演进：

```csharp
public string Name { get; set; }                 // 自动属性
public string Name { get; init; } = "";           // 只能在初始化时赋值
public string Name { get; private set; } = "";    // 外部只读
public double Value => _value * Scale;            // 只读计算属性
```

**上位机配置类几乎全是 `get; set;`**，因为要用 JSON 反序列化。

---

## 7. 继承、接口、多态

```csharp
public interface IModbusMaster : IDisposable
{
    Task<ushort[]> ReadHoldingRegistersAsync(byte unitId, ushort start, ushort count, CancellationToken ct = default);
}

public sealed class ModbusTcpMaster : IModbusMaster { ... }
public sealed class ModbusRtuMaster : IModbusMaster { ... }
```

上层只依赖 `IModbusMaster`：

```csharp
var master = device.Protocol == DeviceProtocol.ModbusTcp
    ? new ModbusTcpMaster(device.Host, device.Port)
    : (IModbusMaster)new ModbusRtuMaster(port, settings);
```

**为什么一定要接口？**

1. 换协议不改上层；
2. 测试可以塞假实现（本仓库 SelfCheck 的 `LoopbackRtuSlave`）；
3. 面试问“你项目怎么分层”，这就是答案。

规则：**优先组合，不要滥用继承**。继承层级超过 2 层就该反思了。

补充：

### interface 接口（最常用，上位机解耦神器）

接口只定义**应该有哪些方法、属性**，**没有字段，默认 public，不能写构造函数**，本身不能实例化。

点：

- 一个类**可以同时实现多个接口**（class ModbusTcp : ICommunication, ILogger）
- 接口不能包含实例字段；.NET 8+ 可以有默认实现
- 面向接口编程：业务层只依赖 `ICommunication`，不用关心底层是 TCP / 串口 / UDP ✅ 好处：切换驱动无痛，单元测试可以写 Mock 设备。

### abstract class 抽象类（有公共逻辑，适合通信基类）

可以有字段、属性、完整实现方法 + 抽象方法（子类必须实现）

- 只能**单继承**：一个类只能继承一个抽象类
- 适合：多个通信驱动共享重复代码（CRC、日志、超时、重试、断连检测）
- 抽象方法不带实现，子类必须 override 重写

---

## 8. 委托与事件：解耦的利器

### 委托能干啥

1. 把方法当成参数传给别的方法（回调）

2. 把方法存到变量里，晚一点再执行（异步、轮询通知）

3. 一个委托可以挂多个方法（多播委托）

   ### 事件

事件是**在委托基础上加了一层保护壳**，只允许【订阅 +=】、【取消订阅 -=】，**外面不能直接覆盖、不能直接 Invoke**。

```csharp
// 委托 = 方法的变量
Action<string> log = Console.WriteLine;//无返回值
Func<int, int> square = x => x * x;
Predicate<double> isHigh = v => v > 100;

委托（delegate）：可以存方法的变量，把**方法当成参数传来传去**
- Action：做一件事，不返回结果。	无返回值，把这个方法当作入参
- Func：算一个东西，带回结果。	最后一个参数是返回值的类型
- Predicate：判断条件，返回 true/false。

// 事件 = 发布/订阅
事件是基于 委托 实现的“订阅/发布机制”。它通常用于让一个对象在某个事情发生时通知其他对象。
事件本质上是对委托字段的封装：外部对象只能订阅 += 或取消订阅 -=，不能直接触发或覆盖事件。
    
public sealed class PollingService// sealed：密封，不让别人继承这个轮询服务
{
    public event Action<IReadOnlyList<TagValue>>? SnapshotReady;
    public event Action<string, bool>? DeviceStateChanged;

    private void Raise(IReadOnlyList<TagValue> values) => SnapshotReady?.Invoke(values);
}
//`=>` 是**表达式体简写**，等价于大括号写法：{ }

// 订阅
service.SnapshotReady += values => { /* 更新界面 */ };
```

**注意**：订阅了就要在退出时 `-=` 取消，否则对象无法释放（内存泄漏），这是上位机常见 bug。

---

## 9. 异常处理

```csharp
try
{
    await master.ConnectAsync(ct);
}
catch (ModbusTimeoutException ex)
{
    Log.Warn($"连接超时：{ex.Message}");       // 可预期：降级/重试
}
catch (Exception ex)
{
    Log.Error("连接失败", ex);                  // 不可预期：记录堆栈
    throw;                                      // 不要 throw ex; 会丢堆栈
}
finally
{
    // 释放资源
}
```

原则：

1. **能处理的才 catch，处理不了就让它冒泡并记日志。**
2. **不要 `catch { }` 空吞**，至少记一条日志。
3. **不要用异常做正常流程**（比如用异常判断“没数据”）。
4. 自定义异常要带上下文：站号、功能码、地址、原始报文。

`using` 自动释放实现了 `IDisposable` 的对象：

```csharp
using var port = new SerialPortNative(settings);
using var master = new ModbusTcpMaster(host, port);
```

补充：
1. 文件头部 `using Xxx;`：导入命名空间，简写类名
2. `using( var x = new Xxx() ){}`：**自动释放资源（串口、TCP、数据库），防止端口占用、连接泄漏**
3. 能用 using 就尽量用！不 Dispose 会坑死你：串口关不掉、TCP 连接占满、重启程序连不上设备

## 10. 泛型

```csharp
public static T Load<T>(string path, Func<T> defaultFactory) where T : class
{
    if (!File.Exists(path)) return defaultFactory();
    return JsonSerializer.Deserialize<T>(File.ReadAllText(path))!;
}

var config = JsonConfigStore.Load<AppConfig>(path, () => new AppConfig());
```

泛型让“配置读取”“缓存”“结果包装”只写一次。约束 `where T : class` 表示引用类型。

---

## 11. async / await（上位机重点）


新生成的 `cts` 会监听两个取消信号，**任意一个触发取消，cts.Token 就会变成取消状态**：
1. 外部传进来的 `ct`：上层要停止轮询、关闭设备（主动取消）
2. 内部自己设置的 `cts.CancelAfter(3000)`：3 秒超时自动取消

```csharp
public async Task<ushort[]> ReadAsync(CancellationToken ct)
{
    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
    cts.CancelAfter(3000);                    // 3 秒超时

    await stream.WriteAsync(frame, cts.Token);
    var header = await ReadExactAsync(7, cts.Token);
    return Parse(header);
}

// 调用
var values = await master.ReadHoldingRegistersAsync(1, 0, 10, ct);
```

必知 5 条：

1. `async void` 只能用于事件处理器，其他一律 `async Task`（否则异常无法捕获）。
2. UI 线程里**不要 `.Result` / `.Wait()`**，会死锁。用 `await`。
3. 库代码用 `ConfigureAwait(false)`，UI 代码不用（需要回到 UI 线程）。
4. 每个长任务都传 `CancellationToken`，否则关不掉。
5. `Task.Run` 用于 CPU 密集；I/O 直接用原生异步 API。

---

## 12. 上位机常见“语言级”坑

| 坑 | 示例 | 正确做法 |
|---|---|---|
| 整数除法 | `1/2 == 0` | `1.0/2` 或 `(double)a/b` |
| 浮点比较 | `0.1+0.2 == 0.3` 为 false | `Math.Abs(a-b) < 1e-9` |
| 字符串比较 | `==` 受区域性影响 | `string.Equals(a, b, StringComparison.Ordinal)` |
| 日期格式 | `ToString()` 依赖系统区域 | 显式 `"yyyy-MM-dd HH:mm:ss"` |
| 数组越界 | `data[count]` | 先判断长度 |
| 闭包捕获 | `for` 循环里 lambda 用 `i` | 循环内复制局部变量 |
| 事件泄漏 | 订阅后不取消 | `Dispose` 时 `-=` |
| 大对象分配 | 每帧 new 大数组 | 复用缓冲区 |

---

## 13. 练习（做完再往下）

1. 写 `ParseHex("01 03 00 00 00 0A")`，返回 `byte[]`，非法字符抛 `FormatException`。
2. 写 `ToHex(byte[])`，输出 `"01 03 00 00 00 0A"`。
3. 用 LINQ 从 `List<TagValue>` 里取“温度 > 100 且质量正常”的位号名。
4. 定义 `PointConfig` 类，用 `System.Text.Json` 序列化/反序列化。
5. 写一个事件：`TemperatureChanged`，订阅后打印新值。
6. 写一个 `async Task<int>` 方法，模拟 1 秒后返回 42，并在控制台 await 它。
7. 把上面任意一段代码故意写出空引用异常，用断点找到它。

生产代码里的对应实现可以参考 `项目/src/Industrial.Core`；练习题答案在独立工程 `练习题答案` 中，不依赖主项目。

> 完整练习答案：`练习题答案/src/Answers.Console/Chapter02/`，每题一个文件（`Exercise01_ParseHex.cs` … `Exercise07_NullReference.cs`）。
> 只跑第 02 章：`dotnet run --project 练习题答案\src\Answers.Console -- 2`
> 依次跑 02/07/10 三章：`dotnet run --project 练习题答案\src\Answers.Console -- all`
> 先自己写，再对照答案；每题都有中文注释和可运行示例。

---

## 14. 本章验收

- [ ] 能说清 `ushort`、`short`、`double`、`decimal` 各用在哪
- [ ] 会写带 `out` 的 `TryParse` 风格方法
- [ ] 能用接口 + 工厂切换两种实现
- [ ] 理解 `event` 的订阅/退订
- [ ] 能写出不会死锁的 `async` 调用
- [ ] 完成 13 节的 7 个练习

下一章：`03 工程化：分层、Git、日志与测试.md`
