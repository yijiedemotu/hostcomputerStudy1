namespace Answers.Wpf.Models;

/// <summary>
/// 趋势图的一个点：采样时间 + 数值。
///
/// record 是 C# 的“数据记录”类型，编译器自动生成构造函数、属性、相等比较等代码。
/// 一行就能替代原来几十行的样板类，非常适合这种纯数据对象。
/// </summary>
public sealed record TrendPoint(DateTime Time, double Value);
