using System.Text;

namespace Exercises.ConsoleApp;

internal static class HexUtil
{
    /// <summary>
    /// 解析 "01 03 00 00 00 0A" / "0x01,0x03" / "0103000000 0A"。
    /// 非法字符和奇数个十六进制字符都抛 FormatException。
    /// </summary>
    public static byte[] ParseHex(string text)
    {
        var cleaned = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (char.IsWhiteSpace(c) || c is ',' or '-' or ';') continue;

            if (c == '0' && i + 1 < text.Length && (text[i + 1] is 'x' or 'X'))
            {
                i++;
                continue;
            }

            if (!Uri.IsHexDigit(c))
                throw new FormatException($"HEX 内容包含非法字符 '{c}'（位置 {i + 1}），只允许 0-9 A-F 和分隔符。");

            cleaned.Append(c);
        }

        if (cleaned.Length == 0) return Array.Empty<byte>();
        if (cleaned.Length % 2 != 0)
            throw new FormatException($"HEX 字符个数必须是偶数，当前 {cleaned.Length} 个。例如 01 03 要写成 0103。");

        var bytes = new byte[cleaned.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
            bytes[i] = Convert.ToByte(cleaned.ToString(i * 2, 2), 16);
        return bytes;
    }

    public static string ToHex(ReadOnlySpan<byte> data)
    {
        if (data.Length == 0) return string.Empty;
        var sb = new StringBuilder(data.Length * 3);
        foreach (var b in data)
        {
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(b.ToString("X2"));
        }
        return sb.ToString();
    }
}
