# 项目 3：WPF 监控看板（MVVM）

用 WPF 重做一版监控界面，重点练 **XAML + 数据绑定 + 命令 + 数据模板 + 自绘**。

## 功能

- KPI 卡片：位号、当前值、单位、质量、报警颜色（DataTrigger）
- 选中卡片 → 趋势图切换（SelectedTag 双向绑定）
- 启动/停止采集（ICommand + CanExecute 控制按钮可用）
- 报警列表（触发/恢复）
- 深色工业风主题（ResourceDictionary 集中管理颜色）
- 手写趋势图（OnRender + DrawingContext）

## 运行

```powershell
python tools\modbus_slave_sim.py      # 终端 1
dotnet run --project src\ScadaDashboard  # 终端 2
```

点"▶ 启动采集"。

> 完整教程（XAML 绑定逐段讲解、MVVM 数据流、主题与自绘、排错）：
> [`../../../文档/19-项目三WPF监控看板完全教程.md`](../../../文档/19-项目三WPF监控看板完全教程.md)

## MVVM 结构

| 文件 | 职责 |
|---|---|
| `MainWindow.xaml` | 布局、样式、绑定 |
| `MainWindow.xaml.cs` | 构造 VM、把事件接到趋势图、关闭时停止采集 |
| `ViewModels/MainViewModel.cs` | Tags、Alarms、SelectedTag、Commands、Status |
| `ViewModels/TagViewModel.cs` | 值/质量/报警状态 |
| `ViewModels/RelayCommand.cs` | ICommand |
| `ViewModels/ObservableObject.cs` | INotifyPropertyChanged |
| `Controls/TrendChartControl.cs` | 自绘趋势图 |

## 关键点

- 采集在后台线程，`SynchronizationContext.Post` 回 UI 线程；
- ViewModel 不引用 `Dispatcher`，也不引用任何控件；
- 卡片颜色由 `IsAlarm`/`IsBad` 驱动，界面无 if 判断。

## 扩展任务

- [ ] 报警声音
- [ ] 曲线十字光标
- [ ] 日间/夜间主题切换
- [ ] 卡片按设备分组
- [ ] 导出曲线 PNG/CSV
