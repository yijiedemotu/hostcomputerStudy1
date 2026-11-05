using Quality = Answers.ConsoleApp.Models.Quality;
using TagValue = Answers.ConsoleApp.Models.TagValue;

namespace Answers.ConsoleApp;

/// <summary>
/// 第 02 章 · 练习 3：用 LINQ 筛选“温度 &gt; 100 且质量正常”的位号名。
///
/// 题目给的 5 条数据里，只有“炉膛温度”和“过热器温度”同时满足：
/// 数值大于 100，并且数据质量是 Good（好）。
///
/// 为什么上位机要学这个：
/// 一个设备往往有几十上百个位号。界面上要显示“哪些点超限”“哪些点质量不好”，
/// 用 LINQ 可以像说话一样描述筛选条件，而不用手写一长串 for 循环。
/// </summary>
internal static class Exercise03_Linq
{
    public static void Run()
    {
        ConsoleUi.Section("练习 3：LINQ 取“温度 > 100 且质量正常”的位号名");

        // List<TagValue> 是“只能装 TagValue 的列表”，比 ArrayList 更安全：
        // 放错类型编译期就会报错，取出来也不需要强制转换。
        //
        // new() { ... } 是对象初始化器：先创建 TagValue，再给属性赋值。
        // Quality.Good 是枚举值，表示“通信正常、数据可信”。
        var values = new List<TagValue>
        {
            new() { DeviceName = "1#炉", TagName = "炉膛温度", Value = 120.5, Quality = Quality.Good },
            new() { DeviceName = "1#炉", TagName = "炉膛温度备份", Value = 118.2, Quality = Quality.Bad },
            new() { DeviceName = "2#炉", TagName = "出口温度", Value = 96.4, Quality = Quality.Good },
            new() { DeviceName = "2#炉", TagName = "过热器温度", Value = 135.7, Quality = Quality.Good },
            new() { DeviceName = "3#炉", TagName = "烟道温度", Value = 101.0, Quality = Quality.Uncertain }
        };

        // LINQ 链式调用，从左到右读：
        // Where：保留满足括号内条件的元素。&& 表示“并且”。
        // Select：把每个 TagValue 投影成它的 TagName（只要名字，不要整条数据）。
        // ToList：把结果真正装进一个 List<string>，方便后面输出。
        var names = values
            .Where(v => v.Value > 100 && v.Quality == Quality.Good)
            .Select(v => v.TagName)
            .ToList();

        ConsoleUi.KeyValue("满足条件的位号", string.Join("、", names));
        ConsoleUi.Note("Where 负责筛选，Select 负责投影；生产代码里建议再加 OrderByDescending(v => v.Value)。");
    }
}
