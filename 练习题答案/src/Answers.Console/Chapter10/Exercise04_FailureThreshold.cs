namespace Answers.ConsoleApp;

/// <summary>
/// 第 10 章 · 练习 4：连续失败 3 次才判离线，成功 1 次立即恢复。
///
/// 题目：实现一个设备在线状态判定器，按失败/成功序列观察状态变化。
///
/// 为什么要这样设计：
/// 现场网络偶尔抖一下很常见。如果失败一次就报警离线，会满屏报警、频繁闪红闪绿。
/// 用“连续失败达到阈值才离线”，可以过滤掉偶发干扰；
/// 而“成功一次立即恢复”，则保证数据恢复后界面能马上回到正常。
///
/// 这种“进门难、出门易”的规则，在报警、断线重连、设备健康度里非常常用。
/// </summary>
internal static class Exercise04_FailureThreshold
{
    public static void Run()
    {
        ConsoleUi.Section("练习 4：连续失败 3 次才离线，成功 1 次立即恢复");

        // 阈值设为 3：连续失败 3 次才翻转成离线。
        var tracker = new DeviceHealthTracker(failureThreshold: 3);

        // 订阅状态变化事件：只有状态真正翻转时才触发（不是每次失败都触发）。
        tracker.StateChanged += (online, failures) =>
        {
            var state = online ? "在线" : "离线";
            ConsoleUi.KeyValue("状态变化", $"{state}（连续失败 {failures} 次）");
        };

        // 模拟一串操作：前 4 次失败，然后成功，再失败两次，再成功。
        var sequence = new[]
        {
            ("失败", false), ("失败", false), ("失败", false), ("失败", false),
            ("成功", true), ("失败", false), ("失败", false), ("成功", true)
        };

        foreach (var (label, success) in sequence)
        {
            if (success) tracker.ReportSuccess();
            else tracker.ReportFailure();

            // 每步都打印当前状态，方便观察“计数增长但还没离线”的过程。
            ConsoleUi.KeyValue(label, $"当前在线：{tracker.IsOnline}，连续失败：{tracker.ConsecutiveFailures}");
        }

        ConsoleUi.Answer("前 3 次失败不会立刻报离线，避免一次干扰就满屏报警；第 3 次才翻转状态；" +
                         "成功一次立即恢复并把失败计数清零。");
    }

    /// <summary>
    /// 设备健康状态判定器。
    /// 初始认为在线（刚启动时还没有失败证据，不必先报离线）。
    /// </summary>
    private sealed class DeviceHealthTracker(int failureThreshold)
    {
        public bool IsOnline { get; private set; } = true;
        public int ConsecutiveFailures { get; private set; }

        /// <summary>
        /// 状态翻转事件，参数：是否在线、当前连续失败次数。
        /// </summary>
        public event Action<bool, int>? StateChanged;

        /// <summary>一次通信成功：清零失败计数；如果之前离线，就恢复在线并通知界面。</summary>
        public void ReportSuccess()
        {
            var wasOffline = !IsOnline;
            ConsecutiveFailures = 0;
            IsOnline = true;

            // 只有“从离线变在线”才触发，避免每次成功都刷一条状态变化。
            if (wasOffline) StateChanged?.Invoke(true, 0);
        }

        /// <summary>一次通信失败：累计计数；达到阈值且当前仍在线时，翻转为离线。</summary>
        public void ReportFailure()
        {
            ConsecutiveFailures++;

            // 已经离线时不重复触发，避免每失败一次就报一次“离线”。
            if (IsOnline && ConsecutiveFailures >= failureThreshold)
            {
                IsOnline = false;
                StateChanged?.Invoke(false, ConsecutiveFailures);
            }
        }
    }
}
