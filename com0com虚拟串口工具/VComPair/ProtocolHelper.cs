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
