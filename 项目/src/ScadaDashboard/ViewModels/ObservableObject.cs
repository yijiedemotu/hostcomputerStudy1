using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ScadaDashboard.ViewModels;

/// <summary>
/// 手写 MVVM 基类：只做一件事——属性变更通知。
///
/// WPF 的数据绑定靠的是 <see cref="INotifyPropertyChanged"/> 接口：
/// 只要属性 setter 里触发 PropertyChanged 事件，界面就会自动重新读取这个属性。
/// 教程里常说 CommunityToolkit.Mvvm 的 [ObservableProperty] 方便，
/// 但先手写一遍才知道它到底帮你生成了什么代码（面试高频）。
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    /// <summary>
    /// 属性值发生变化时触发。WPF 绑定引擎订阅的正是这个事件，
    /// 事件里的属性名告诉它“哪一个绑定需要刷新”。
    /// 注意：事件允许为 null（还没有任何界面订阅时），所以调用处要用 ?.。
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 属性 setter 的通用写法：值真的变了才赋值并通知。
    /// ref T field 传的是字段的引用，所以这里可以直接改调用方的字段。
    /// [CallerMemberName] 会自动把“调用它的属性名”填进来，
    /// 于是每个 setter 里不需要手写 "ValueText" 这样的字符串，改名也不会漏。
    /// 返回值：true 表示这次确实发生了变化。
    /// </summary>
    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        // 用 EqualityComparer 而不是 ==，兼容 string、double 和自定义类型。
        // 值相同就直接返回，避免无意义的界面刷新（采集很快时这一步很重要）。
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;

        field = value;                     // 先真正写入字段
        OnPropertyChanged(propertyName);   // 再通知界面
        return true;
    }

    /// <summary>
    /// 触发属性变更通知。子类需要手工通知“计算属性”时也可以直接调用，
    /// 例如某个属性由另外几个属性算出来，本身没有对应字段。
    /// </summary>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
