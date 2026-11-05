using System.Text.Encodings.Web;
using System.Text.Json;

namespace Exercises.ConsoleApp;

/// <summary>
/// 第 02 章 · 练习 4：把配置对象序列化成 JSON，再反序列化回来。
///
/// 题目：定义一个 PointConfig 类，用 System.Text.Json 实现：
/// 对象 → JSON 文本（序列化），JSON 文本 → 对象（反序列化）。
///
/// 为什么上位机要学这个：
/// 现场的点位名称、寄存器地址、量程、单位、报警上限，不能写死在代码里。
/// 常见做法是存进 appsettings.json，程序启动时读出来。改配置不用重新编译。
///
/// 本文件里的 PointConfig 只保留题目要求的字段，方便单独阅读。
/// </summary>
internal static class Exercise04_Json
{
    public static void Run()
    {
        ConsoleUi.Section("练习 4：定义 PointConfig，用 System.Text.Json 序列化 / 反序列化");

        // 创建一个配置对象，并给属性赋初值。
        // 注意 HighLimit 的类型是 double?：问号表示“可空”，不填就是 null（没有报警上限）。
        var config = new PointConfig
        {
            Name = "1#炉 炉膛温度",
            Address = 0,
            DataType = "Int16",
            Scale = 0.1,
            Unit = "℃",
            HighLimit = 950
        };

        // JsonSerializerOptions 是序列化时的“选项”：
        // WriteIndented = true：输出带换行和缩进，方便人看；
        // UnsafeRelaxedJsonEscaping：让中文原样输出，而不是变成 \u7089 这种转义。
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        // 序列化：对象 → JSON 字符串。
        var json = JsonSerializer.Serialize(config, options);
        Console.WriteLine("  序列化结果：");
        // Split('\n') 按换行切成多行，每行前面补 4 个空格，只是为了让控制台输出整齐。
        foreach (var line in json.Split('\n'))
            Console.WriteLine("    " + line.TrimEnd());

        // 反序列化：JSON 字符串 → 对象。泛型 <PointConfig> 告诉它要还原成什么类型。
        // ?? 是空合并运算符：如果左边是 null，就执行右边的 throw，避免后面用到空对象。
        var back = JsonSerializer.Deserialize<PointConfig>(json, options)
                   ?? throw new InvalidOperationException("反序列化返回 null");

        ConsoleUi.KeyValue("反序列化 Name", back.Name);
        ConsoleUi.KeyValue("反序列化 Scale / Unit", $"{back.Scale} / {back.Unit}");
        ConsoleUi.Ok("往返一致，配置类可用于 appsettings.json。");
    }

    /// <summary>
    /// 一个点位（测点）的配置。
    /// 属性名会成为 JSON 的键名，例如 Name 对应 "Name"。
    /// 每个属性后面的 { get; set; } 表示可读可写，JSON 库才能给它赋值。
    /// </summary>
    private sealed class PointConfig
    {
        /// <summary>位号名称，例如“1#炉 炉膛温度”。</summary>
        public string Name { get; set; } = "";

        /// <summary>Modbus 寄存器地址（从 0 开始）。</summary>
        public ushort Address { get; set; }

        /// <summary>数据类型，例如 Int16、UInt16、Float32。</summary>
        public string DataType { get; set; } = "Int16";

        /// <summary>缩放系数：原始值乘以它才是工程值，例如 0.1。</summary>
        public double Scale { get; set; } = 1;

        /// <summary>工程单位，例如 ℃、MPa、rpm。</summary>
        public string Unit { get; set; } = "";

        /// <summary>报警上限；null 表示不配置上限。</summary>
        public double? HighLimit { get; set; }
    }
}
