using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Exercises.Wpf.ViewModels;

/// <summary>
/// 所有 ViewModel 的基类，负责“属性变了要通知界面”这件事。
///
/// MVVM 的核心是数据绑定：
/// XAML 里写 Text="{Binding Temperature}"，界面不会自己盯着变量看；
/// 它靠 INotifyPropertyChanged 事件得知“Temperature 变了”，然后刷新显示。
///
/// 如果忘了发通知，变量确实变了，但界面仍然显示旧值——这是 WPF 新手最常见的问题。
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    // 属性变化事件。? 表示可能没有订阅者（没有绑定到界面的对象就是 null）。
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 设置字段并自动发通知。返回 true 表示值确实变了，false 表示新旧值相同。
    ///
    /// [CallerMemberName]：编译器自动把“调用这个方法的属性名”填进来，
    /// 所以属性里只写 SetProperty(ref _x, value)，不用手写 "X" 字符串。
    /// </summary>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        // EqualityComparer 对值类型比较值，对引用类型调用 Equals。
        // 相同就不赋值、不发通知，避免无意义的界面刷新。
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>手动通知某个属性变化，用于“计算属性”（例如 FullName 依赖 FirstName）。</summary>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
