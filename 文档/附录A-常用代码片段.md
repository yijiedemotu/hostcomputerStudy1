# 附录 A 常用代码片段速查

> 复制即用。所有片段都与本仓库代码风格一致（C# 12 / .NET 8）。

---

## A1. 十六进制与字节

```csharp
// 字节 → "AA BB CC"
string hex = string.Join(' ', data.Select(b => b.ToString("X2")));
string hex2 = Convert.ToHexString(data);                 // "AABBCC"

// "AA BB CC" / "0xAA,0xBB" → 字节
static byte[] ParseHex(string text)
{
    var sb = new StringBuilder();
    for (var i = 0; i < text.Length; i++)
    {
        var c = text[i];
        if (c is ' ' or '\r' or '\n' or '\t' or ',' or '-' or ';') continue;
        if (c == '0' && i + 1 < text.Length && (text[i + 1] is 'x' or 'X')) { i++; continue; }
        if (!Uri.IsHexDigit(c)) throw new FormatException($"非法字符 '{c}'");
        sb.Append(c);
    }
    if (sb.Length % 2 != 0) throw new FormatException("HEX 长度必须为偶数");
    return Convert.FromHexString(sb.ToString());
}

// int ↔ 4 字节（大端）
byte[] be = BitConverter.GetBytes(value);
if (BitConverter.IsLittleEndian) Array.Reverse(be);
```

---

## A2. CRC16（Modbus）

```csharp
public static ushort Crc16(ReadOnlySpan<byte> data)
{
    ushort crc = 0xFFFF;
    foreach (var b in data)
    {
        crc ^= b;
        for (var i = 0; i < 8; i++)
            crc = (crc & 1) != 0 ? (ushort)((crc >> 1) ^ 0xA001) : (ushort)(crc >> 1);
    }
    return crc;
}

// 追加 CRC（低字节在前）
var frame = new byte[data.Length + 2];
data.CopyTo(frame);
var crc = Crc16(data);
frame[^2] = (byte)(crc & 0xFF);
frame[^1] = (byte)(crc >> 8);
```

---

## A3. 累加和 / 异或校验

```csharp
byte sum = 0;
foreach (var b in data) sum += b;          // 累加和（取低 8 位）

byte xor = 0;
foreach (var b in data) xor ^= b;          // 异或校验

// LRC（Modbus ASCII）：累加取反 +1
byte lrc = (byte)(-(data.Sum(b => (int)b)) & 0xFF);
```

---

## A4. 32 位数据拆装（ABCD）

```csharp
// 两个寄存器 → uint
uint value = ((uint)regs[0] << 16) | regs[1];

// uint → 两个寄存器
ushort hi = (ushort)(value >> 16);
ushort lo = (ushort)(value & 0xFFFF);

// float ↔ 寄存器（大端）
var bits = BitConverter.SingleToInt32Bits(123.45f);
var hiF = (ushort)(bits >> 16);
var loF = (ushort)(bits & 0xFFFF);
```

---

## A5. 打开串口（官方 SerialPort）

```csharp
using System.IO.Ports;

var port = new SerialPort("COM3", 9600, Parity.None, 8, StopBits.One)
{
    ReadTimeout = 500,
    WriteTimeout = 500,
    DtrEnable = true,
    RtsEnable = true
};
try
{
    port.Open();
    port.Write(frame, 0, frame.Length);
}
catch (UnauthorizedAccessException)
{
    // 端口被占用
}
catch (IOException ex)
{
    // 端口不存在 / 设备拔出
}
```

---

## A6. TCP 客户端（带超时）

```csharp
using var client = new TcpClient { NoDelay = true };
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

await client.ConnectAsync(host, port, cts.Token);
var stream = client.GetStream();

await stream.WriteAsync(frame, cts.Token);

var buffer = new byte[260];
var read = await stream.ReadAsync(buffer, cts.Token);
var response = buffer.AsSpan(0, read).ToArray();
```

---

## A7. 断线重连循环

```csharp
while (!ct.IsCancellationRequested)
{
    try
    {
        await ConnectAsync(ct);
        await PollAsync(ct);
    }
    catch (OperationCanceledException) { break; }
    catch (Exception ex)
    {
        Log.Warn($"通信中断：{ex.Message}，2 秒后重连");
        await Task.Delay(2000, ct);
    }
}
```

---

## A8. 线程安全队列

```csharp
private readonly ConcurrentQueue<HistoryRecord> _queue = new();

_queue.Enqueue(record);

while (_queue.TryDequeue(out var item))
    list.Add(item);
```

---

## A9. 跨线程更新界面

```csharp
// WinForms
if (InvokeRequired) BeginInvoke(new Action(() => label.Text = text));
else label.Text = text;

// WPF
Dispatcher.BeginInvoke(new Action(() => label.Text = text));

// 通用（构造时捕获）
private readonly SynchronizationContext _ui = SynchronizationContext.Current!;
_ui.Post(_ => label.Text = text, null);
```

---

## A10. JSON 配置读写

```csharp
using System.Text.Json;

var options = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNameCaseInsensitive = true,
    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
};

var json = JsonSerializer.Serialize(config, options);
File.WriteAllText("appsettings.json", json);

var loaded = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText("appsettings.json"), options);
```

---

## A11. CSV 写入（Excel 不乱码）

```csharp
using var writer = new StreamWriter(path, append: true, new UTF8Encoding(true));
writer.WriteLine("时间,位号,数值,单位");
writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss},{name},{value:0.###},{unit}");
```

---

## A12. SQLite 增删改查

```csharp
using Microsoft.Data.Sqlite;

using var conn = new SqliteConnection("Data Source=data/history.db");
conn.Open();

using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = "INSERT INTO History(TagId, Ts, Value) VALUES($t, $ts, $v)";
    cmd.Parameters.AddWithValue("$t", tagId);
    cmd.Parameters.AddWithValue("$ts", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
    cmd.Parameters.AddWithValue("$v", value);
    cmd.ExecuteNonQuery();
}

using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = "SELECT Ts, Value FROM History WHERE TagId=$t ORDER BY Ts DESC LIMIT 100";
    cmd.Parameters.AddWithValue("$t", tagId);
    using var reader = cmd.ExecuteReader();
    while (reader.Read()) Console.WriteLine($"{reader.GetString(0)} {reader.GetDouble(1)}");
}
```

---

## A13. 单实例运行

```csharp
using var mutex = new Mutex(true, "UpperComputer.SingleInstance", out var createdNew);
if (!createdNew)
{
    MessageBox.Show("程序已经在运行。");
    return;
}
Application.Run(new MainForm());
```

---

## A14. 开机自启（注册表）

```csharp
using Microsoft.Win32;

const string runKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
using var key = Registry.CurrentUser.OpenSubKey(runKey, writable: true);
key?.SetValue("UpperComputer", $"\"{Application.ExecutablePath}\"");
```

---

## A15. 导出 DataGridView 为 CSV

```csharp
var sb = new StringBuilder();
var headers = grid.Columns.Cast<DataGridViewColumn>().Select(c => c.HeaderText);
sb.AppendLine(string.Join(",", headers));

foreach (DataGridViewRow row in grid.Rows)
{
    if (row.IsNewRow) continue;
    var cells = row.Cells.Cast<DataGridViewCell>().Select(c => c.Value?.ToString() ?? "");
    sb.AppendLine(string.Join(",", cells.Select(v => v.Contains(',') ? $"\"{v}\"" : v)));
}

File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
```

---

## A16. 计时与性能

```csharp
var sw = Stopwatch.StartNew();
DoWork();
sw.Stop();
Console.WriteLine($"耗时 {sw.ElapsedMilliseconds} ms");

// 采集周期扣除耗时
var wait = Math.Max(50, intervalMs - (int)sw.ElapsedMilliseconds);
await Task.Delay(wait, ct);
```

---

## A17. 读取 INI（老项目兼容）

```csharp
[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
private static extern int GetPrivateProfileString(
    string section, string key, string def, StringBuilder retVal, int size, string filePath);

var sb = new StringBuilder(256);
GetPrivateProfileString("Com", "Port", "COM1", sb, 256, "config.ini");
```

---

## A18. 安全删除旧文件

```csharp
var cutoff = DateTime.Now.Date.AddDays(-keepDays);
foreach (var file in Directory.EnumerateFiles(dir, "*.csv"))
{
    if (File.GetLastWriteTime(file) < cutoff)
    {
        try { File.Delete(file); } catch (IOException) { /* 被占用，跳过 */ }
    }
}
```
