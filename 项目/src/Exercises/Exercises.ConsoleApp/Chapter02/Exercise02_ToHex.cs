namespace Exercises.ConsoleApp;

/// <summary>
/// 第 02 章 · 练习 2：把 byte[] 还原成带空格的十六进制字符串。
///
/// 题目：new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x0A } → "01 03 00 00 00 0A"。
///
/// 这是练习 1 的反方向操作，常用于：
/// - 把将要发出的报文写进日志；
/// - 在界面的“接收区”显示设备原始数据；
/// - 排查问题时，肉眼比对帧内容。
/// </summary>
internal static class Exercise02_ToHex
{
    public static void Run()
    {
        ConsoleUi.Section("练习 2：ToHex(byte[]) → \"01 03 00 00 00 0A\"");

        // 0x 开头表示十六进制：0x0A 等于十进制的 10。
        // byte 只能保存 0~255，一个元素就是一个字节。
        var data = new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x0A };

        // HexUtil.ToHex 会逐个字节转成两位大写十六进制，并在中间补空格。
        var text = HexUtil.ToHex(data);
        ConsoleUi.KeyValue("输出", text);

        // == 对字符串比较的是“内容是否完全相同”，不是比较两个变量是不是同一个对象。
        // 三目运算符 条件 ? 值1 : 值2：条件成立取“是”，否则取“否”。
        ConsoleUi.KeyValue("是否与题目一致", text == "01 03 00 00 00 0A" ? "是" : "否");
        ConsoleUi.Note("实际项目可写成 Convert.ToHexString(data) 后再每两个字符插一个空格；手写循环更适合理解。");
    }
}
