using System.Windows.Input;

namespace Exercises.Wpf.ViewModels;

/// <summary>
/// 一个最简化的 ICommand 实现，用来把“按钮点击”绑定到 ViewModel 的方法。
///
/// 为什么需要它：
/// WinForms 里按钮有 Click 事件，直接写事件处理器就行；
/// WPF 的 MVVM 要求 ViewModel 不引用按钮/窗口，所以用命令（Command）代替事件。
/// XAML 写 Command="{Binding RefreshCommand}"，点击时 WPF 调用 Execute。
///
/// 两个构造函数的区别：
/// - Action execute：无参数命令，最常用；
/// - Action&lt;object?&gt; execute：带参数命令，例如列表里的“删除这一行”。
/// </summary>
public sealed class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;             // 真正要执行的动作
    private readonly Func<object?, bool>? _canExecute;     // 是否允许执行（可选）

    /// <summary>无参数版本：把 Action 包装成带 object 参数的委托。</summary>
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute is null ? null : _ => canExecute())
    {
    }

    /// <summary>带参数版本：参数从 XAML 的 CommandParameter 传入。</summary>
    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    // ICommand 要求的事件：命令“可用状态”改变时通知按钮刷新（变灰/恢复）。
    public event EventHandler? CanExecuteChanged;

    // 没提供 canExecute 时默认为 true，也就是按钮始终可用。
    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => _execute(parameter);

    /// <summary>手动触发“可用状态变了”，让绑定的按钮重新查询 CanExecute。</summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
