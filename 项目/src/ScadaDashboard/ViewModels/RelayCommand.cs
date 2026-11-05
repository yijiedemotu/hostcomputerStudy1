using System.Windows.Input;

namespace ScadaDashboard.ViewModels;

/// <summary>
/// 最简 ICommand 实现：把“按钮点击”变成“ViewModel 里的方法”，
/// 界面代码里不再出现业务逻辑（这就是 MVVM 与 WinForms 事件写的最大区别）。
///
/// 用法：在 ViewModel 里 new RelayCommand(Start, () => !IsRunning)，
/// XAML 里写 Command="{Binding StartCommand}"。按钮会自动调用 CanExecute
/// 决定自己是否可点，并在 CanExecuteChanged 触发时重新判断。
/// 这个类不支持命令参数（parameter），因为本项目所有按钮都不需要。
/// </summary>
public sealed class RelayCommand : ICommand
{
    // 真正要执行的方法，由 ViewModel 在构造时传进来。
    private readonly Action _execute;

    // 判断“现在能不能执行”的方法，可为空；为空表示永远可执行。
    private readonly Func<bool>? _canExecute;

    /// <summary>
    /// 构造命令。
    /// execute 不能为空——没有要执行的动作，这个命令就没有意义。
    /// canExecute 省略时视为始终可执行。
    /// </summary>
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>
    /// WPF 在按钮显示前、以及收到 CanExecuteChanged 后都会调用这里，
    /// 返回 false 时按钮自动变灰（例如采集运行中，“开始”按钮就不可点）。
    /// 参数 parameter 本项目用不到，直接忽略。
    /// </summary>
    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    /// <summary>按钮被点击（且 CanExecute 为 true）时执行这里。</summary>
    public void Execute(object? parameter) => _execute();

    /// <summary>通知 WPF 重新调用 CanExecute，刷新按钮的可用状态。</summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// 由 ViewModel 在状态变化后手动调用。
    /// 手写版的关键点：CanExecuteChanged 不会自己触发，
    /// 所以 MainViewModel 在 IsRunning 改变后会显式调用它。
    /// </summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
