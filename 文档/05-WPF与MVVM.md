# 05 WPF 与 MVVM

> 本章目标：掌握 WPF 的 XAML、数据绑定、命令、样式与自定义绘制，能独立写一个 MVVM 看板。
> 参考实现：`项目/src/ScadaDashboard`（完全手写，不依赖 MVVM 框架）。

---

## 1. WPF 和 WinForms 的本质区别

| | WinForms | WPF |
|---|---|---|
| 界面描述 | 代码（Designer.cs） | XAML（声明式） |
| 渲染 | GDI+，像素 | DirectX，矢量 |
| 布局 | 坐标 + Dock/Anchor | 测量/排列（Measure/Arrange） |
| 数据 | 事件里改控件 | 绑定到 ViewModel |
| 外观 | 属性 | 样式 + 模板，可换肤 |
| 学习曲线 | 平缓 | 陡峭（绑定/模板/依赖属性） |

WPF 的核心思想：**UI 是数据的投影**。你改数据，界面自己变；而不是“找到控件改它的 Text”。

---

## 2. XAML 基础

```xml
<Window x:Class="ScadaDashboard.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="看板" Height="800" Width="1400">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>   <!-- 按内容 -->
            <RowDefinition Height="*"/>      <!-- 占剩余 -->
            <RowDefinition Height="200"/>    <!-- 固定 -->
        </Grid.RowDefinitions>

        <TextBlock Grid.Row="0" Text="标题" FontSize="22"/>
        <Border Grid.Row="1" Background="#171A21"/>
    </Grid>
</Window>
```

常用布局容器：

| 容器 | 适用 | 说明 |
|---|---|---|
| `Grid` | 主布局 | 行列 + 跨行跨列，最常用 |
| `StackPanel` | 线性排列 | 横/竖排，不自动换行 |
| `WrapPanel` | 卡片流 | 自动换行（KPI 卡片） |
| `DockPanel` | 顶/底/左/右 | LastChildFill |
| `Canvas` | 绝对定位 | 画图、拖拽 |

布局属性：`Margin`（外）、`Padding`（内）、`HorizontalAlignment`、`VerticalAlignment`、`Width/Height`、`MinWidth/MaxWidth`。

> 经验：**90% 的界面一个 Grid 就能搭完**。不要嵌套五层 StackPanel。

---

## 3. 数据绑定（WPF 的灵魂）

```xml
<TextBlock Text="{Binding StatusText}"/>
<TextBox Text="{Binding IpAddress, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"/>
<TextBlock Text="{Binding Value, StringFormat={}{0:0.###}}"/>
<Button Content="启动" Command="{Binding StartCommand}"/>
```

绑定四要素：

| 要素 | 说明 | 例 |
|---|---|---|
| Source | 数据源 | `DataContext`（最常用） |
| Path | 属性路径 | `SelectedTag.Name` |
| Mode | 方向 | `OneWay`/`TwoWay`/`OneTime` |
| Converter | 值转换 | bool → 颜色 |

### 3.1 DataContext

```csharp
public MainWindow()
{
    InitializeComponent();
    DataContext = new MainViewModel();   // 整个窗口的绑定源
}
```

### 3.2 INotifyPropertyChanged

```csharp
public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
}
```

**没有这个接口，界面永远不会刷新**——这是新手第一个大坑。

### 3.3 集合绑定

```csharp
public ObservableCollection<TagViewModel> Tags { get; } = new();
```

`ObservableCollection` 在增删时通知界面；元素属性变化靠元素自己实现 `INotifyPropertyChanged`。

### 3.4 ICommand

```csharp
public sealed class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;
    public RelayCommand(Action execute, Func<bool>? canExecute = null) { ... }

    public bool CanExecute(object? p) => _canExecute?.Invoke() ?? true;
    public void Execute(object? p) => _execute();
    public event EventHandler? CanExecuteChanged;
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
```

ViewModel：

```csharp
StartCommand = new RelayCommand(Start, () => !IsRunning);
StopCommand  = new RelayCommand(Stop,  () => IsRunning);
```

按钮禁用状态自动跟随 `IsRunning`，**界面里一行 if 都不用写**。

---

## 4. 数据模板：列表怎么长这样

```xml
<ListBox ItemsSource="{Binding Tags}" SelectedItem="{Binding SelectedTag, Mode=TwoWay}">
    <ListBox.ItemTemplate>
        <DataTemplate>
            <StackPanel Width="196">
                <TextBlock Text="{Binding Name}" Foreground="#9AA3B2"/>
                <StackPanel Orientation="Horizontal">
                    <TextBlock Text="{Binding ValueText}" FontSize="28" FontWeight="Bold"/>
                    <TextBlock Text="{Binding Unit}" VerticalAlignment="Bottom"/>
                </StackPanel>
            </StackPanel>
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
```

`DataTrigger` 按状态改颜色：

```xml
<DataTemplate.Triggers>
    <DataTrigger Binding="{Binding IsAlarm}" Value="True">
        <Setter TargetName="ValueText" Property="Foreground" Value="#FF6B6B"/>
    </DataTrigger>
    <DataTrigger Binding="{Binding IsBad}" Value="True">
        <Setter TargetName="ValueText" Property="Foreground" Value="#6B7280"/>
    </DataTrigger>
</DataTemplate.Triggers>
```

> 这比 WinForms 的 `CellFormatting` 干净得多：**状态 → 样式**，而不是“在每个事件里判断”。

---

## 5. 样式与控件模板

```xml
<Style x:Key="ToolButton" TargetType="Button">
    <Setter Property="Background" Value="#222833"/>
    <Setter Property="Foreground" Value="#E6E8EC"/>
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="Button">
                <Border Background="{TemplateBinding Background}" CornerRadius="6" Padding="16,7">
                    <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                </Border>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

WPF 里“圆角按钮”就是重写模板，而不是像 WinForms 那样自绘。

换肤：把颜色都放 `ResourceDictionary`，运行时替换即可（工业软件常见“日间/夜间/高对比度”）。

---

## 6. 自定义绘制：OnRender

本项目 `Controls/TrendChartControl.cs`：

```csharp
public sealed class TrendChartControl : FrameworkElement
{
    protected override void OnRender(DrawingContext dc)
    {
        dc.DrawRoundedRectangle(BackgroundBrush, null, new Rect(0, 0, ActualWidth, ActualHeight), 8, 8);
        // 网格、刻度、报警线、折线
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(first, false, false);
            ctx.LineTo(next, true, false);
        }
        dc.DrawGeometry(null, LinePen, geometry);
    }
}
```

要点：

- `InvalidateVisual()` 触发重绘；
- `FormattedText` 画文字（要注意 `PixelsPerDip`，否则高 DPI 模糊）；
- `Freeze()` 冻结 Brush/Pen，提升性能；
- 折线用 `StreamGeometry`，比一个个 `DrawLine` 快。

---

## 7. MVVM 的分工

```
Model        数据与业务（AppConfig、TagValue、Modbus 协议）
ViewModel    界面状态 + 命令（Tags、SelectedTag、StartCommand）
View         XAML + 少量代码（绑定、自定义控件挂接）
```

规则：

1. View 不直接访问数据库/串口；
2. ViewModel 不引用 `System.Windows.Controls`；
3. View 与 ViewModel 通过 `DataContext` 绑定；
4. 复杂交互（弹窗、拖拽）可用附加行为/服务接口，但别把业务写进代码后置。

本项目的边界：

| 文件 | 职责 |
|---|---|
| `MainViewModel.cs` | 采集服务、Tags、Alarms、命令、状态文字 |
| `MainWindow.xaml` | 布局、样式、绑定 |
| `MainWindow.xaml.cs` | 把 ViewModel 事件接到趋势图、关闭时停止采集 |
| `TrendChartControl.cs` | 纯绘制 |

**代码后置只有 30 行**，这就是 MVVM 的效果。

---

## 8. 跨线程：SynchronizationContext

```csharp
public MainViewModel()
{
    _uiContext = SynchronizationContext.Current!;   // 构造时在 UI 线程
}

private void OnSnapshotReady(IReadOnlyList<TagValue> values)
{
    _uiContext.Post(_ => ApplySnapshot(values), null);   // 回 UI 线程
}
```

WPF 里也可以用 `Dispatcher.Invoke`，但 ViewModel 里出现 `Dispatcher` 就说明耦合了 UI 框架。**用 SynchronizationContext 更干净。**

---

## 9. 性能与常见坑

| 坑 | 现象 | 解决 |
|---|---|---|
| 绑定到普通字段 | 界面不刷新 | 用属性 + INotifyPropertyChanged |
| 忘记 `Mode=TwoWay` | 输入不回写 | 显式设置 |
| 每次 new Brush | 内存上涨、卡 | `Freeze()` 并复用 |
| 大列表不虚拟化 | 滚动卡 | 用 `VirtualizingStackPanel` |
| `TextBlock` 绑复杂属性 | 频繁重算 | 缓存格式化结果 |
| 在 UI 线程 `Thread.Sleep` | 界面冻结 | `await Task.Delay` |
| 事件订阅未取消 | 内存泄漏 | 弱事件/手动退订 |

---

## 10. WPF 与 WinForms 互操作

老项目迁移时常见：

```xml
<WindowsFormsHost>
    <wf:DataGridView x:Name="grid"/>
</WindowsFormsHost>
```

需要引用 `WindowsFormsIntegration`。**能不用就不用**，两套渲染混排会有 DPI 和层级问题。

---

## 11. 选型建议

| 场景 | 推荐 |
|---|---|
| 维护老项目、快速交付 | WinForms |
| 新项目、界面复杂、要换肤 | WPF |
| 大表格（10 万行） | WinForms（虚拟模式）或 WPF + 虚拟化 |
| 触屏一体机 | WPF（缩放/动画好），注意按钮做大 |
| 团队只会 WinForms | 别硬上 WPF，交接成本高 |

---

## 12. 练习

1. 在 WPF 看板里加“报警声音”（`MediaPlayer` 或 `SystemSounds`）。
2. 给趋势图加鼠标移动显示“时间 + 数值”的十字光标。
3. 用 `DataTemplateSelector` 让不同设备显示不同卡片样式。
4. 把主题色抽到 `ResourceDictionary`，实现“日间/夜间”切换。
5. 用 `CollectionViewSource` 给报警表加排序/筛选。

> 完整练习答案：`练习题答案/src/Answers.Wpf/Windows/`，5 道练习分别是 `AlarmSoundWindow`、`CrosshairWindow`、`DeviceCardsWindow`、`ThemeWindow`、`AlarmListWindow`。
> 运行 `dotnet run --project 练习题答案\src\Answers.Wpf`，在主窗口选择练习；每道练习都是独立的 `Window.xaml`。
> 双击任意 `.xaml` 可直接查看设计器；数据绑定在设计时显示占位值，运行后由 ViewModel 填充。
> 答案用独立窗口演示，MVVM 写法和 `ScadaDashboard` 一致；实际项目里可以直接搬 ViewModel、DataTemplate 和资源字典。

---

## 13. 本章验收

- [ ] 能解释 `DataContext`、`Binding`、`INotifyPropertyChanged` 的关系
- [ ] 能实现一个 `RelayCommand` 并控制按钮可用状态
- [ ] 能用 `DataTemplate` + `DataTrigger` 做状态颜色
- [ ] 能写一个 `OnRender` 自定义控件
- [ ] 知道 ViewModel 里为什么不该出现 `Dispatcher`
- [ ] 完成第 12 节至少 2 个练习

下一章：`06 通信基础：串口、TCP、字节与协议.md`
