using System.Windows;

namespace Exercises.Wpf.Services;

/// <summary>主题模式：夜间 / 日间。</summary>
public enum ThemeMode
{
    Dark,
    Light
}

/// <summary>
/// 运行时替换主题资源字典。
///
/// 换肤原理：
/// 1. App.xaml 启动时把 Themes/Dark.xaml 合进应用资源；
/// 2. 界面颜色全部写成 DynamicResource（例如 Background="{DynamicResource AppBackgroundBrush}"）；
/// 3. 切换主题时，找到合并字典里的旧主题，替换成 Light.xaml；
/// 4. DynamicResource 会自动重新取值，窗口不用重建，颜色立刻变化。
///
/// 对比 StaticResource：它只在加载时取一次值，之后替换字典不会跟着变。
/// 所以“支持换肤”的界面必须用 DynamicResource。
/// </summary>
public static class ThemeManager
{
    public static ThemeMode Current { get; private set; } = ThemeMode.Dark;

    public static void Apply(ThemeMode mode)
    {
        var app = Application.Current;
        if (app is null) return;   // 设计器里可能没有 Application，直接返回

        var dictionaries = app.Resources.MergedDictionaries;

        // 找到当前已合并的 Dark.xaml 或 Light.xaml。
        // 通过文件名的后缀判断，避免写死它在集合中的下标。
        var oldTheme = dictionaries.FirstOrDefault(dictionary =>
            dictionary.Source is not null &&
            (dictionary.Source.OriginalString.EndsWith("Dark.xaml", StringComparison.OrdinalIgnoreCase) ||
             dictionary.Source.OriginalString.EndsWith("Light.xaml", StringComparison.OrdinalIgnoreCase)));

        var fileName = mode == ThemeMode.Dark ? "Dark.xaml" : "Light.xaml";

        // pack://application:,,,/程序集名;component/路径 是 WPF 访问“程序内嵌资源”的标准 URI。
        // 不能写成普通相对路径，因为运行时当前目录不一定是程序目录。
        var next = new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/Exercises.Wpf;component/Themes/{fileName}", UriKind.Absolute)
        };

        if (oldTheme is null)
        {
            // 没找到旧主题（例如 App.xaml 没配置），就插到最前面。
            dictionaries.Insert(0, next);
        }
        else
        {
            // 在原位置替换，保持字典顺序不变。
            var index = dictionaries.IndexOf(oldTheme);
            dictionaries[index] = next;
        }

        Current = mode;
    }
}
