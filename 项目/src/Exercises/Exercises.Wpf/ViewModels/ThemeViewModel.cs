using System.Windows.Input;
using Exercises.Wpf.Services;

namespace Exercises.Wpf.ViewModels;

/// <summary>
/// 第 05 章练习 4：日夜主题切换的 ViewModel。
///
/// ViewModel 只记录“当前是不是夜间主题”，并调用 ThemeManager 替换资源字典。
/// 具体颜色在 Themes/Dark.xaml 和 Themes/Light.xaml 里，ViewModel 不碰颜色值。
///
/// 这样做的好处：
/// - 想改配色，只改主题 XAML，不用改 C#；
/// - 增加第三套主题（例如高对比度）时，只需加一个字典和分支。
/// </summary>
public sealed class ThemeViewModel : ObservableObject
{
    private bool _isDark = ThemeManager.Current == ThemeMode.Dark;

    public ThemeViewModel()
    {
        // 三个命令分别对应：切换、强制夜间、强制日间。
        ToggleCommand = new RelayCommand(Toggle);
        UseDarkCommand = new RelayCommand(() => SetTheme(true));
        UseLightCommand = new RelayCommand(() => SetTheme(false));
    }

    public bool IsDark => _isDark;

    public string ToggleText => _isDark ? "切换到日间主题" : "切换到夜间主题";
    public string CurrentThemeText => _isDark ? "夜间主题 · Themes/Dark.xaml" : "日间主题 · Themes/Light.xaml";

    public ICommand ToggleCommand { get; }
    public ICommand UseDarkCommand { get; }
    public ICommand UseLightCommand { get; }

    private void Toggle() => SetTheme(!_isDark);

    private void SetTheme(bool dark)
    {
        if (_isDark == dark) return;

        _isDark = dark;

        // 真正换肤：替换 Application.Resources 里的主题字典。
        ThemeManager.Apply(dark ? ThemeMode.Dark : ThemeMode.Light);

        // 三个计算属性都依赖 _isDark，必须逐个通知界面刷新。
        OnPropertyChanged(nameof(IsDark));
        OnPropertyChanged(nameof(ToggleText));
        OnPropertyChanged(nameof(CurrentThemeText));
    }
}
