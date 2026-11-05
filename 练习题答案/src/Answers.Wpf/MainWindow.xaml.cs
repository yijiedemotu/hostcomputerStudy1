using System.Windows;
using Answers.Wpf.Windows;

namespace Answers.Wpf;

/// <summary>
/// 主窗口：只负责导航。
///
/// 和旧版不同，这里没有把 5 个练习塞进一个导航壳，
/// 而是让每道题拥有自己的 Window.xaml：
/// 设计器能直接看到整扇窗口，运行时也可以单独打开、单独关闭。
///
/// 打开窗口统一用 ShowDialog()：
/// 模态窗口会锁住主窗口，关闭后才能进入下一题，
/// 避免多个数据看板同时跑定时器。
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OpenAlarmSound_Click(object sender, RoutedEventArgs e) => Open(new AlarmSoundWindow());

    private void OpenCrosshair_Click(object sender, RoutedEventArgs e) => Open(new CrosshairWindow());

    private void OpenDeviceCards_Click(object sender, RoutedEventArgs e) => Open(new DeviceCardsWindow());

    private void OpenTheme_Click(object sender, RoutedEventArgs e) => Open(new ThemeWindow());

    private void OpenAlarmList_Click(object sender, RoutedEventArgs e) => Open(new AlarmListWindow());

    private void Open(Window window)
    {
        window.Owner = this;
        window.ShowDialog();
    }
}
