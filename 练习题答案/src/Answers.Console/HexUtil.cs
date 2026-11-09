using System.Text;

namespace Answers.ConsoleApp;

internal static class HexUtil
{
    /// <summary>
    /// 解析 "01 03 00 00 00 0A" / "0x01,0x03" / "0103000000 0A"。
    /// 非法字符和奇数个十六进制字符都抛 FormatException。
    /// </summary>
    public static byte[] ParseHex(string text)
    {
        var cleaned = new StringBuilder(text.Length);
        ///`cleaned` 用来存放**清洗之后纯十六进制字符**（去掉所有分隔符、去掉 `0x` 标记）
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            // 遇到分隔符直接跳过
            if (char.IsWhiteSpace(c) || c is ',' or '-' or ';') continue;

            // 处理 0x / 0X 标记：遇到 0，下一位是 x/X，两个一起跳过
            if (c == '0' && i + 1 < text.Length && (text[i + 1] is 'x' or 'X'))
            {
                i++;
                continue;
            }

            // 校验：剩下的字符必须是合法16进制（0-9 A-F a-f）
            if (!Uri.IsHexDigit(c))
                throw new FormatException($"HEX 内容包含非法字符 '{c}'（位置 {i + 1}），只允许 0-9 A-F 和分隔符。");

            cleaned.Append(c);
        }

        if (cleaned.Length == 0) return Array.Empty<byte>();
        if (cleaned.Length % 2 != 0)
            throw new FormatException($"HEX 字符个数必须是偶数，当前 {cleaned.Length} 个。例如 01 03 要写成 0103。");

        //用新字节数组存放解析结果，每两个十六进制字符对应一个字节
        var bytes = new byte[cleaned.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
            bytes[i] = Convert.ToByte(cleaned.ToString(i * 2, 2), 16);
        //结果如bytes = [0x01, 0x03]
        return bytes;
    }

    /// ToHex —— byte [] → 格式化十六进制字符串
    public static string ToHex(ReadOnlySpan<byte> data)
    {
        if (data.Length == 0) return string.Empty;
        //每个字节占 2 个字符 + 1 个空格，预留足够的空间
        var sb = new StringBuilder(data.Length * 3);
        foreach (var b in data)
        {
            if (sb.Length > 0) sb.Append(' ');
            //`"X2"` 含义：大写十六进制，**固定 2 位，不足前面补 0**（`0x01` → `"01"`，而不是 `"1"`）
            sb.Append(b.ToString("X2"));
        }
        return sb.ToString();
    }
}
