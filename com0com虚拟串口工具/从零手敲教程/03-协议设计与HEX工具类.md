# 03 · 协议设计与 HEX 工具类（里程碑 M3）

> **本章目标**：在写服务端之前，先把"线上到底跑什么字节"定下来；再写 `ProtocolHelper.cs`，并把发送按钮接上 HEX 解析做自检。
> **验收**：在发送框输入 `01 03 00 0A` 点「发送」→ 弹出「4 字节 / 01 03 00 0A」；输入奇数个或非法字符 → 弹出中文错误提示。

---

## 1. 为什么需要"握手"

服务端只监听一个 TCP 端口（9527），但要有**两个不同的端点** `VCOM1` / `VCOM2`。
客户端连上来时必须先说清楚"我要占哪个端点"，否则服务端不知道把字节转给谁。

另外还要解决三件事：

| 需求 | 协议里怎么解决 |
|---|---|
| 声明端点名 | `HELLO|VCOM1|标签` |
| 告诉客户端"你连上了" | 回 `+OK|VCOM1` |
| 端口被占用 / 端点名写错 | 回 `-ERR|原因`，客户端把它当异常抛给界面 |
| 区分"控制信息"和"串口数据" | 用**第一行**（`\n` 结束）做握手；第一行之后的字节**全部当作裸数据**，不解析、不分包 |

### 报文格式（逐字节定义）

```
客户端 → 服务端（连接建立后立即发送，一行，\n 结束，UTF-8）：

    HELLO|VCOM1|SerialAssistant\n
    └─┬──┘ └─┬─┘ └──────┬─────┘
     命令   端点名     来源标签（只用于界面显示，可含中文）

服务端 → 客户端（一行，UTF-8）：

    +OK|VCOM1\n          接受
    -ERR|VCOM1 已被占用（同一串口只能被一个程序打开，请先断开旧连接）\n   拒绝

握手成功之后：

    任意原始字节，双向透明转发，不做分包、不加帧头、不做转义
```

### 三个设计取舍（面试常问）

1. **为什么用"一行文本"做握手，而不是二进制包头？**
   调试方便 —— 用 PowerShell / `telnet` 手工就能连上敲一行验证，不需要写客户端。第 04 章就靠这个做验收。

2. **为什么用 UTF-8 而不是 ASCII？**
   `-ERR` 里的中文原因和 `内置终端` 标签用 ASCII 编码会全部变成 `?`。
   本项目第一版就踩过：`Encoding.ASCII.GetBytes("端口已被占用")` → 收到 `?????`。
   读行时也要**按字节收集再 `Encoding.UTF8.GetString`**，不能逐字节转 `char`（那对多字节 UTF-8 同样会乱码）。

3. **为什么握手之后不做分包？**
   真实串口返回的是"字节流"，一次 `Read` 拿到多少字节是不确定的（粘包/半包）。
   如果服务端在这里按帧切分，就**改变了串口语义**，上层协议（Modbus RTU）反而会出错。
   所以只做字节搬运，切帧交给业务层。

---

## 2. 完整通讯时序

```
   客户端 A (VCOM1)              PairHub (9527)              客户端 B (VCOM2)
        │                              │                              │
        │── ① TCP connect ────────────────►│                              │
        │── ② HELLO|VCOM1|Tag ────────────►│                              │
        │                              │ 检查会话表：VCOM1 空闲 → 登记       │
        │◄─ ③ +OK|VCOM1 ─────────────────│                              │
        │                              │◄──── ④ TCP connect ───────────│
        │                              │◄──── ⑤ HELLO|VCOM2|Tag ───────│
        │                              │ 登记 VCOM2，补发缓存 ───────────►│
        │                              │── ⑥ +OK|VCOM2 ─────────────────►│
        │                              │                              │
        │── ⑦ 写 01 03 00 0A ───────────►│                              │
        │                              │ 触发 Traffic 事件（监控窗口）    │
        │                              │── ⑧ 原样写入 B 的 socket ──────►│
        │                              │                              │ ⑨ Read 得到 4 字节
        │                              │                              │
        │                              │◄─ ⑩ 断开 / Stop ──────────────│
        │◄─ 连接关闭 ────────────────────│ 清理会话、取消令牌             │
```

> 注意 ④~⑥：如果 B 还没连，⑦ 的数据不会丢，服务端会缓存到 B 上线再补发（M4 实现）。

---

## 3. 新建 `ProtocolHelper.cs`（最终版，一次敲定）

```csharp
using System.Text;

namespace VComPair;

/// <summary>HEX / 文本互转，两个内置终端和监控窗口共用。</summary>
internal static class ProtocolHelper
{
    /// <summary>把发送框内容按 HEX 或 UTF-8 文本转成字节。</summary>
    public static byte[] BuildBytes(string text, bool hex) =>
        hex ? ParseHex(text) : Encoding.UTF8.GetBytes(text);

    /// <summary>
    /// 解析 "01 03 00 00"、"0x01,0x03"、"01030000" 等写法。
    /// 与串口调试助手 MainForm.ParseHex 的规则保持一致。
    /// </summary>
    public static byte[] ParseHex(string text)
    {
        var cleaned = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c is ' ' or '\r' or '\n' or '\t' or ',' or '-' or ';') continue;
            if (c == '0' && i + 1 < text.Length && (text[i + 1] is 'x' or 'X')) { i++; continue; }
            if (!Uri.IsHexDigit(c))
                throw new FormatException($"HEX 含有非法字符 '{c}'（位置 {i + 1}），只允许 0-9 A-F 和分隔符。");
            cleaned.Append(c);
        }

        if (cleaned.Length == 0) return [];
        if (cleaned.Length % 2 != 0)
            throw new FormatException($"HEX 字符个数必须是偶数，当前 {cleaned.Length} 个。例如 01 03 应写成 0103。");

        var result = new byte[cleaned.Length / 2];
        for (var i = 0; i < result.Length; i++)
            result[i] = Convert.ToByte(cleaned.ToString(i * 2, 2), 16);
        return result;
    }

    /// <summary>"01 03 0A" 形式，用于监控窗口。</summary>
    public static string ToHex(byte[] data)
    {
        var sb = new StringBuilder(data.Length * 3);
        foreach (var b in data) sb.Append(b.ToString("X2")).Append(' ');
        return sb.ToString().TrimEnd();
    }

    /// <summary>不可打印字符统一显示为 '.'，避免监控窗口出现乱码控制符。</summary>
    public static string ToAscii(byte[] data)
    {
        var sb = new StringBuilder(data.Length);
        foreach (var b in data) sb.Append(b is >= 32 and < 127 ? (char)b : '.');
        return sb.ToString();
    }
}
```

### 逐段讲解

| 代码 | 知识点 |
|---|---|
| `internal static class` | `internal` = 只在当前程序集可见；`static` = 纯工具方法，不需要实例 |
| `BuildBytes(text, hex)` | 一个入口统一"文本 / HEX"两条路径，调用方不用分支 |
| `c is ' ' or '\r' or ...` | C# 9 **模式匹配**，`or` 组合多个常量，比一长串 `\|\|` 清晰 |
| `0x` 前缀处理 | 遇到 `0` 后面跟 `x/X`，把 `i++` 多跳一位，等价于跳过整个 `0x` |
| `Uri.IsHexDigit(c)` | BCL 现成的十六进制字符判断，不用自己写 `c >= '0' && ...` |
| `return [];` | C# 12 **集合表达式**，等价于 `Array.Empty<byte>()` |
| `cleaned.Length % 2 != 0` | 两个十六进制字符 = 1 字节，奇数必然无法成对 |
| `Convert.ToByte(str, 16)` | 按 16 进制把两位字符串转 byte；非法字符前面已过滤 |
| `b.ToString("X2")` | 固定两位大写十六进制，不足补 0（`0x0A` → `0A`） |
| `b is >= 32 and < 127` | 关系模式匹配；可打印 ASCII 范围外的显示为 `.`，防止控制符弄乱日志 |

### 为什么用 `StringBuilder` 而不是 `string +=`

`ToHex` 在监控窗口每一帧都会调用。字符串拼接会不断分配新对象，帧率高时 GC 压力大；`StringBuilder` 预分配 `data.Length * 3` 容量，一次成型。

---

## 4. 把发送按钮接上 HEX 自检（M3 改动）

打开 `TerminalPanel.cs`，把第 02 章占位的 `SendCurrent()` **整体替换**为：

```csharp
    private void SendCurrent()
    {
        try
        {
            var data = ProtocolHelper.BuildBytes(_send.Text, _hexSend.Checked);
            if (data.Length == 0) return;
            MessageBox.Show(this,
                $"解析成功：{data.Length} 字节\n" +
                $"HEX：{ProtocolHelper.ToHex(data)}\n" +
                $"ASCII：{ProtocolHelper.ToAscii(data)}",
                "HEX 解析自检", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (FormatException ex)
        {
            MessageBox.Show(this, ex.Message, "发送内容格式错误",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
```

> 这里故意先弹窗而不是真的发数据 —— 让你在**没有服务端**的情况下就能验证 HEX 解析是否正确。
> 第 05 章会把它换成真正发送 + 回显的版本。

---

## 5. 动手实验（把下表全试一遍）

| 发送框输入 | 勾 `HEX 发` | 预期 |
|---|---|---|
| `01 03 00 0A` | ✅ | 4 字节 `01 03 00 0A` |
| `0103000A` | ✅ | 4 字节（连续写法也支持） |
| `0x01,0x03` | ✅ | 2 字节 `01 03` |
| `01-03;00` | ✅ | 3 字节 `01 03 00` |
| `01 03 0` | ✅ | 报错「HEX 字符个数必须是偶数，当前 5 个」 |
| `01 GG` | ✅ | 报错「非法字符 'G'（位置 4）」 |
| 空字符串 | ✅ | 什么都不发生（`data.Length == 0` 直接 return） |
| `你好 VCOM` | ❌ | 6~10 字节（按 UTF-8 编码），ASCII 列显示 `....` |

> **重要观察**：文本模式下 `Encoding.UTF8.GetBytes("你好")` 是 6 字节，不是 2 字节。
> 这就是串口调试里"中文乱码"的根源之一 —— 一个汉字 3 字节可能被拆到两次 `Read` 里。

---

## 6. 验收清单

- [ ] `ProtocolHelper.cs` 编译通过，`Ctrl+Shift+B` 0 错误
- [ ] 上表 8 个用例结果全部符合预期
- [ ] 能说清握手三行报文各自的用途
- [ ] 能说清为什么握手用 UTF-8、为什么读行要按字节收集
- [ ] 能说清"握手后不分包"的理由（保持串口字节流语义）
- [ ] 能解释 `Uri.IsHexDigit` / `Convert.ToByte(s,16)` / `X2` 各自的作用

---

**下一章**：[04 · 服务端 PairHub 逐行实现](04-服务端PairHub逐行实现.md) —— 把骨架替换成真正的 TCP 服务端，并用 PowerShell 手工验证协议。
