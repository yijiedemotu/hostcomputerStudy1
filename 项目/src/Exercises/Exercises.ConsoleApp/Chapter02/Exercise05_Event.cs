namespace Exercises.ConsoleApp;

/// <summary>
/// 第 02 章 · 练习 5：用事件实现“温度变化后通知订阅者”。
///
/// 题目：定义 TemperatureChanged 事件，订阅后打印新值；再退订，验证不再收到通知。
///
/// 为什么上位机要学这个：
/// 采集线程读到新数据后，界面、报警模块、历史存盘都想知道“有新值了”。
/// 事件就是“发布—订阅”机制：采集器只管发通知，谁关心谁订阅，双方不用互相引用。
/// </summary>
internal static class Exercise05_Event
{
    public static void Run()
    {
        ConsoleUi.Section("练习 5：事件 TemperatureChanged，订阅后打印新值");

        // 创建“发布者”：温度传感器。
        var sensor = new TemperatureSensor();

        // 定义“订阅者要执行的动作”。
        // EventHandler<T> 是 .NET 内置的委托类型，签名固定为 (发送者, 事件参数)。
        // 这里的 (_, e) 中，下划线表示“这个参数我不用”，e 里装着新的温度值。
        // => 是 Lambda 表达式，可以理解成“一个没有名字的小方法”。
        EventHandler<TemperatureChangedEventArgs> handler = (_, e) =>
            Console.WriteLine($"    [订阅者] 收到温度变化：{e.NewValue:F1} ℃");

        // += 表示“订阅”：把 handler 挂到事件上。
        sensor.TemperatureChanged += handler;
        sensor.Update(25.5);
        sensor.Update(26.0);

        // -= 表示“退订”：把 handler 从事件上摘下来。
        // 退订后再更新，订阅者不会收到通知，因此控制台不会打印 27.0。
        sensor.TemperatureChanged -= handler;
        sensor.Update(27.0);
        ConsoleUi.Note("退订后再 Update(27.0) 没有输出，说明事件订阅必须成对出现；窗体 Dispose 时忘记 -= 会泄漏。");
    }

    /// <summary>
    /// 发布者：模拟一个温度传感器。
    /// 它对外只暴露一个事件，内部 Update 被调用时触发事件。
    /// </summary>
    private sealed class TemperatureSensor
    {
        // event 关键字表示“这是事件，只能由本类触发，外部只能 += / -=”。
        // 末尾的 ? 表示可能没有人订阅，这时值为 null。
        public event EventHandler<TemperatureChangedEventArgs>? TemperatureChanged;

        /// <summary>收到一个新温度值后，通知所有订阅者。</summary>
        public void Update(double value) =>
            // ?.Invoke 的意思是：如果事件不为 null（确实有人订阅），才调用它。
            // 不写 ?. 而直接 Invoke，没人订阅时会抛 NullReferenceException。
            TemperatureChanged?.Invoke(this, new TemperatureChangedEventArgs(value));
    }

    /// <summary>
    /// 事件参数：把“新温度值”从发布者传给订阅者。
    /// 按 .NET 惯例继承 EventArgs。
    /// </summary>
    private sealed class TemperatureChangedEventArgs(double newValue) : EventArgs
    {
        /// <summary>本次采集到的新温度。</summary>
        public double NewValue { get; } = newValue;
    }
}
