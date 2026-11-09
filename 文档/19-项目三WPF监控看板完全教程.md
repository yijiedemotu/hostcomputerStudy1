# 19 · 项目三：WPF 监控看板完全教程（MVVM）

> 用 WPF 把项目二的数据重做一版监控界面，重点练 **XAML + 数据绑定 + ICommand + DataTemplate + 自绘控件**。
> 配套代码：`项目/src/ScadaDashboard/`。总览见 [14-项目实战](14-项目实战三个项目讲解.md)，WPF 基础见 [05-WPF与MVVM](05-WPF与MVVM.md)。

---

## 0. 这篇教什么

| 模块 | 学到什么 |
|---|---|
| 跑通与操作 | 模拟器 + 看板，卡片跳数、选中切换曲线、报警列表 |
| XAML 布局 | 5 行 Grid、WrapPanel 卡片墙、DataTemplate、DataTrigger、DataGrid、状态栏 |
| MVVM 三件套 | `ObservableObject` / `RelayCommand` / `MainViewModel`，ViewModel 不引用任何控件 |
| 跨线程 | 构造时捕获 `SynchronizationContext`，后台线程 `.Post` 回 UI 线程 |
| 自绘 | `TrendChartControl.OnRender` + `DrawingContext` 画曲线 |
| 与 WinForms 项目对照 | 同一套 `PollingService`，两种 UI 架构怎么接 |

前置：`文档/05`（WPF 与 MVVM）、`文档/07`（Modbus）、`文档/10`（多线程）。

---

## 1. 5 分钟跑通

```powershell
cd D:\上位机\上位机学习大全\项目

# 终端 1：Modbus TCP 从站模拟器（保持运行）
python tools\modbus_slave_sim.py

# 终端 2：WPF 看板
dotnet run --project src\ScadaDashboard
```

点右上角 **「▶ 启动采集」**：

- KPI 卡片（模温、注射压力、周期时间、累计产量、排气压力、排气温度）开始跳数；
- 状态文字变成 `采集运行中 · 127.0.0.1:502`；
- 点任意一张卡片 → 下方趋势图切换成该位号并持续绘制；
- 状态栏"N 个快照 · 6 个位号"和"最后更新 HH:mm:ss.fff"持续刷新；
- 数值超限时卡片变红，报警列表新增一条"触发"，恢复时再记一条"恢复"。

> 看板连的是 `127.0.0.1:502`，配置**写死在 `MainViewModel.CreateDemoConfig()`**（不是 JSON），
> 这是刻意的：让它专注演示 MVVM，不与项目二的配置体系耦合。

前置条件与项目二相同：502 被占用就换端口，同时改 `CreateDemoConfig()` 里的 `Port`。

---

## 2. XAML 布局逐块讲解

`MainWindow.xaml` 用一个 **5 行 Grid**：

| 行 | 高度 | 内容 |
|---|---|---|
| 0 | Auto | 标题 + `StatusText` + 「▶ 启动采集」/「■ 停止」按钮 |
| 1 | Auto | KPI 卡片区（`ListBox` + `WrapPanel`） |
| 2 | `*` | 趋势图 `TrendChartControl`（占满剩余空间） |
| 3 | 200 | 报警记录 `DataGrid` |
| 4 | Auto | 状态栏：快照数 / 最后更新 / 提示 |

所有 `{Binding ...}` 都指向 `MainWindow.DataContext`，也就是 `MainViewModel`。

### 2.1 按钮：用 Command 而不是 Click

```xml
<TextBlock Text="{Binding StatusText}" .../>
<Button Content="▶ 启动采集" Command="{Binding StartCommand}" .../>
<Button Content="■ 停止" Command="{Binding StopCommand}" .../>
```

`MainWindow.xaml.cs` 里**没有一个按钮点击事件**。
`StartCommand` / `StopCommand` 是 `RelayCommand`，各自带 `CanExecute` 条件：

```csharp
StartCommand = new RelayCommand(Start, () => !IsRunning);
StopCommand  = new RelayCommand(Stop,  () => IsRunning);
```

`IsRunning` 变化时调 `RaiseCanExecuteChanged()`，按钮自动变灰/恢复可点。

### 2.2 卡片墙：ListBox + WrapPanel + DataTemplate

```xml
<ListBox ItemsSource="{Binding Tags}"
         SelectedItem="{Binding SelectedTag, Mode=TwoWay}"
         ItemContainerStyle="{StaticResource TagCardStyle}">
    <ListBox.ItemsPanel>
        <ItemsPanelTemplate><WrapPanel/></ItemsPanelTemplate>
    </ListBox.ItemsPanel>
    <ListBox.ItemTemplate>
        <DataTemplate>
            <StackPanel Width="196">
                <TextBlock Text="{Binding Name}" .../>
                <TextBlock x:Name="ValueText" Text="{Binding ValueText}" FontSize="28" .../>
                <TextBlock Text="{Binding Unit}" .../>
                <Ellipse x:Name="Dot" .../>
                <TextBlock x:Name="QualityText" Text="{Binding Quality}" .../>
            </StackPanel>
            <DataTemplate.Triggers>
                <DataTrigger Binding="{Binding IsAlarm}" Value="True"> ... 变红 ... </DataTrigger>
                <DataTrigger Binding="{Binding IsBad}"   Value="True"> ... 变灰 ... </DataTrigger>
            </DataTemplate.Triggers>
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
```

三个关键点：

1. `ItemsPanel` 换成 `WrapPanel`，卡片才会自动换行，而不是一列到底；
2. `SelectedItem` 用 `Mode=TwoWay`：点卡片 → `ViewModel.SelectedTag` 变；ViewModel 改选中项 → 高亮也跟着变；
3. **颜色完全由数据驱动**：`DataTrigger` 根据 `IsAlarm` / `IsBad` 换颜色，C# 里没有一句 `if (报警) 改颜色`。

### 2.3 报警表与状态栏

```xml
<DataGrid ItemsSource="{Binding Alarms}" AutoGenerateColumns="False" IsReadOnly="True">
    <DataGrid.Columns>
        <DataGridTextColumn Header="时间" Binding="{Binding Time, StringFormat=HH:mm:ss}" .../>
        ... 位号 / 数值 / 级别 / 动作 ...
    </DataGrid.Columns>
</DataGrid>
```

状态栏绑定 `SampleCountText` 与 `LastUpdateText`，用 `StringFormat=最后更新 {0}` 加前缀。

### 2.4 主题资源集中在 App.xaml

| 资源键 | 颜色 | 用途 |
|---|---|---|
| `BgBrush` | `#12141A` | 窗口背景 |
| `PanelBrush` | `#171A21` | 卡片/面板背景 |
| `BorderBrush` | `#2A2F3A` | 边框 |
| `TextBrush` | `#E6E8EC` | 主文字 |
| `MutedBrush` | `#9AA3B2` | 次要文字 |
| `AccentBrush` | `#00DCA0` | 强调色/正常状态圆点 |
| `AlarmBrush` | `#FF6B6B` | 报警红 |

样式：`ToolButton`（按钮）、`TagCardStyle`（卡片容器）、`GridHeaderStyle`（表头）。
**换主题只改 App.xaml 一个文件**，界面不用动 —— 这是 WPF 相对 WinForms 的一大优势。

---

## 3. MVVM 三件套

| 文件 | 职责 |
|---|---|
| `ViewModels/ObservableObject.cs` | `INotifyPropertyChanged` 基类，`Set(ref field, value)` 在值变化时发通知并返回 bool |
| `ViewModels/RelayCommand.cs` | `ICommand` 实现，持 `Action` + `Func<bool>`，`RaiseCanExecuteChanged()` 刷新按钮可用状态 |
| `ViewModels/TagViewModel.cs` | 一张卡片的数据：Name / ValueText / Unit / Quality / Device / Key / IsAlarm / IsBad / LowLimit / HighLimit，`Update(TagValue)` 批量刷新 |
| `ViewModels/MainViewModel.cs` | 所有界面状态的唯一来源：Tags、Alarms、SelectedTag、三个 Command、状态文字 |
| `MainWindow.xaml` / `.xaml.cs` | 视图：布局 + 绑定 + 把"画图"这类纯 UI 动作接上 |
| `Controls/TrendChartControl.cs` | `OnRender` 自绘趋势图，不绑 ViewModel |

**边界**：ViewModel 里没有 `Dispatcher`、没有 `Button`、没有 `MessageBox`、没有控件引用。
它只暴露"属性 + 命令 + 两个事件（`SelectedTagChanged` / `PointAdded`）"，视图自己决定怎么画。

### 3.1 `MainWindow.xaml.cs` 只做三件事

```csharp
_viewModel = new MainViewModel();
DataContext = _viewModel;                       // ① 挂 DataContext

_viewModel.SelectedTagChanged += tag =>         // ② 选中变化 → 重置曲线
    Trend.Reset(tag?.Name ?? "未选择位号", tag?.Unit ?? "", tag?.LowLimit, tag?.HighLimit);

_viewModel.PointAdded += (tag, value, time) => // ③ 新点 → 追加到曲线
{
    if (ReferenceEquals(tag, _viewModel.SelectedTag)) Trend.AddPoint(time, value);
};

Loaded += (_, _) => _viewModel.SelectedTag = _viewModel.Tags[0];  // 默认选中第一张卡
```

关闭窗口时 `OnClosing` 调 `_viewModel.DisposeAsync().AsTask().Wait(1500)`，停掉采集线程，避免进程残留后台任务。

---

## 4. 数据全链路

```
PollingService（后台线程，每台设备一个 Task）
   │ ReadHoldingRegistersAsync → Decode → RaiseSnapshot
   ▼
MainViewModel.OnSnapshotReady(IReadOnlyList<TagValue>)
   │ _uiContext.Post(_ => ApplySnapshot(values), null)   ← 切回 UI 线程
   ▼
ApplySnapshot（UI 线程）
   ├─ tag.Update(value)                    → 属性通知 → 卡片文本/颜色自动刷新
   ├─ 若 Quality.Good 且是当前选中项 → PointAdded 事件 → Trend.AddPoint
   ├─ 报警边沿检测 → Alarms.Insert(0, ...) → DataGrid 自动刷新
   └─ SampleCountText / LastUpdateText → 状态栏刷新
```

### 4.1 跨线程为什么用 `SynchronizationContext`

```csharp
public MainViewModel()
{
    _uiContext = SynchronizationContext.Current ?? new SynchronizationContext();  // 必须在 UI 线程构造
    ...
}

private void OnSnapshotReady(IReadOnlyList<TagValue> values)
    => _uiContext.Post(_ => ApplySnapshot(values), null);
```

- `Post` 是**异步**的，不阻塞采集线程；
- ViewModel 不引用 `Dispatcher`，所以它可以脱离 WPF 单独测试；
- 构造时捕获上下文是前提：**千万不要在后台线程里 new ViewModel**，否则 `SynchronizationContext.Current` 为 null。

### 4.2 报警边沿检测

```csharp
var previous = _alarmState.TryGetValue(tag.Key, out var level) ? level : AlarmLevel.None;
if (previous == value.Alarm) continue;        // 状态没变，不记
_alarmState[tag.Key] = value.Alarm;
if (value.Alarm != AlarmLevel.None) Alarms.Insert(0, 触发记录);
else                                Alarms.Insert(0, 恢复记录);
while (Alarms.Count > 200) Alarms.RemoveAt(Alarms.Count - 1);
```

和项目二同一套思路，只是集合换成 `ObservableCollection<AlarmItem>`，界面自动跟着变。

### 4.3 演示配置

`CreateDemoConfig()` 写死两台设备、六个位号，与模拟器对应：

| 设备 | 站号 | 位号（地址 / 类型 / 倍率 / 上下限） |
|---|---|---|
| 1#注塑机 | 1 | 模温(0 / Int16 / 0.1 / 40~220℃)、注射压力(1 / UInt16 / 0.01 / ≤120MPa)、周期时间(2 / UInt16 / 0.1s)、累计产量(3 / **UInt32 占两个寄存器**) |
| 2#空压机 | 2 | 排气压力(0 / UInt16 / 0.01 / ≤12MPa)、排气温度(1 / Int16 / 0.1 / ≤95℃) |

`PollIntervalMs = 500`，`Host = 127.0.0.1`，`Port = 502`。
**UInt32 占两个寄存器**是很容易忽略的点：地址 3 用了 2 个寄存器，配置时别把下一个点位写到地址 4 之前。

---

## 5. 自绘趋势图

`Controls/TrendChartControl.cs` 是一个手写 WPF 控件：

- 继承 `FrameworkElement`/`Control`，重写 `OnRender(DrawingContext)`；
- `Reset(标题, 单位, 低限, 高限)` 清空并套用新位号的量程与报警线；
- `AddPoint(time, value)` 追加数据点并 `InvalidateVisual()` 触发重绘；
- 报警上下限画成虚线，曲线按当前数据范围自动缩放。

它**不绑定 ViewModel**，而是由 `MainWindow.xaml.cs` 订阅 `PointAdded` / `SelectedTagChanged` 后直接调用方法。
这是 MVVM 里常见的例外：**纯绘图控件保留命令式接口**，避免为了画图把一堆坐标塞进 ViewModel。

> 项目二用的是 WinForms + GDI+ 双缓冲手绘；项目三是 WPF + `DrawingContext`。
> 两者思路一致（维护点集合 → 坐标变换 → 画线），正好对照学习。

---

## 6. 与项目二（WinForms）的对照

| 维度 | 项目二 WinForms | 项目三 WPF |
|---|---|---|
| 数据源 | `BindingList<TagRow>` | `ObservableCollection<TagViewModel>` |
| 更新通知 | `INotifyPropertyChanged`（`TagRow.Update`） | `ObservableObject.Set` + 属性通知 |
| 用户操作 | `button_Click` 事件 | `ICommand`（`RelayCommand`） |
| 按钮可用状态 | 手动 `btnStart.Enabled = false` | `CanExecute` + `RaiseCanExecuteChanged` |
| 跨线程 | `BeginInvoke` | `SynchronizationContext.Post` |
| 样式/颜色 | `CellFormatting` 里写 C# 判断 | `DataTrigger` 在 XAML 里声明 |
| 主题 | 散落在各控件属性 | `App.xaml` 的 `ResourceDictionary` 集中管理 |
| 曲线 | `TrendChart.cs`（GDI+ 双缓冲） | `TrendChartControl.cs`（`OnRender`） |
| 配置 | `appsettings.json` | 写死在 `CreateDemoConfig()` |

**同一套 `PollingService` 被两个项目复用**，这正是 `Industrial.Core` 分层的价值：
UI 技术换了一整套，采集/协议/模型代码一行没改。

---

## 7. 排错表

| 现象 | 检查顺序 |
|---|---|
| 点「启动采集」没反应 | 按钮是否可点（`IsRunning`/`CanExecute`）→ 模拟器是否在跑 → 502 端口 → 输出窗口有没有绑定错误 |
| 卡片值一直是 `--` | 采集是否启动 → `StatusText` 显示什么 → 站号/地址与模拟器是否一致 |
| 数值不对 | `Scale` / `Order` / `DataType`；UInt32 占两个寄存器，别覆盖相邻地址 |
| 卡片不变红 | 检查 `TagViewModel.IsAlarm` 是否被 `Update()` 正确设置；`DataTrigger` 的 `Binding` 路径是否正确 |
| 曲线不动 | 是否选中了卡片（`Loaded` 会默认选第一张）→ 数据是否 `Quality.Good` → `PointAdded` 是否触发 |
| 报警刷屏 | 正常只在状态翻转时记一条；若仍刷屏，检查 `_alarmState` 的键 `tag.Key` 是否稳定 |
| 界面卡顿 | `ApplySnapshot` 必须在 UI 线程但要做轻量工作；`Post` 频率过高时可加节流 |
| 报"必须在 UI 线程创建 ViewModel"类问题 | `new MainViewModel()` 必须在窗口构造（UI 线程）里执行 |
| 关闭窗口后进程残留 | `OnClosing` 里 `DisposeAsync().AsTask().Wait(1500)` 别删；它会停掉后台采集 |
| XAML 绑定不生效 | 看 Visual Studio"输出"窗口的 `System.Windows.Data Error`；九成是属性名拼错或忘了 `DataContext` |

---

## 8. 扩展任务

- [ ] **报警声音**：`SystemSounds` / `MediaPlayer`，在 `Alarms.Insert` 时播放
- [ ] **曲线十字光标**：鼠标移动显示"时间 + 数值"，练习 `OnRender` + 命中测试
- [ ] **日间/夜间主题切换**：再加一个 `ResourceDictionary`，运行时替换 `Application.Current.Resources`
- [ ] **卡片按设备分组**：`CollectionViewSource` + `GroupStyle`
- [ ] **导出曲线 PNG/CSV**：`RenderTargetBitmap` 把控件渲染成图片
- [ ] **把配置改成读 JSON**：复用 `JsonConfigStore`，与项目二共用 `appsettings.json`
- [ ] **加"通信故障"横幅**：订阅 `DeviceStateChanged`，断线时在顶部显示橙色提示
- [ ] **单元测试 ViewModel**：用假 `PollingService` 或假主站，验证报警边沿与 `CanExecute`

---

## 9. 验收清单

- [ ] 能在无 PLC 情况下跑通看板全链路
- [ ] 能指出 XAML 里所有 `{Binding}` 的数据来源（`DataContext = MainViewModel`）
- [ ] 能解释 `Mode=TwoWay` 在 `SelectedItem` 上的双向作用
- [ ] 能说清 `DataTrigger` 为什么比"在 C# 里改颜色"更符合 MVVM
- [ ] 能复述 `SynchronizationContext.Post` 的跨线程链路，并说明为什么不能在后台线程构造 VM
- [ ] 能讲清 `RelayCommand` 的 `CanExecute` 如何自动禁用/启用按钮
- [ ] 能对照说出 WinForms 版与 WPF 版在 6 个维度上的差异
- [ ] 能解释"为什么 UI 技术换了，Industrial.Core 一行没改"

---

**回到总览**：[14-项目实战三个项目讲解](14-项目实战三个项目讲解.md) ·
[17-项目一串口调试助手完全教程](17-项目一串口调试助手完全教程.md) ·
[18-项目二Modbus采集监控完全教程](18-项目二Modbus采集监控完全教程.md)
