# 练习题答案 · 原生窗口版

这套答案是**独立工程**，和 `项目/src` 下的三个主项目（串口助手、Modbus 监视、SCADA 看板）完全分开，也不引用 `Industrial.Core`。因此可以单独打开、单独运行、单独改，不会影响主项目。

这一版专门解决“代码能跑，但设计器里看不到窗口样式”的问题：

- **WinForms**：每道题一个 `Form`，布局、控件、表格列、Timer、NotifyIcon 全部写在 `Form.Designer.cs` 里，设计器可以直接看到完整界面；
- **WPF**：每道题一个独立的 `Window.xaml`，整个布局都在 XAML 里，双击 XAML 就能看到窗口；
- **Console**：18 道逻辑练习保留，直接运行看输出，适合先理解代码。

---

## 一、打开解决方案

用 Visual Studio 2022（17.8 或更高）或 Rider 打开：

```
练习题答案.sln
```

> 目录里同时有一个 `练习题答案.slnx`，内容和 `.sln` 相同，是 VS 新版的解决方案格式。老版本 VS 打不开 `.slnx` 时，打开 `.sln` 即可。

第一次打开后先“生成解决方案”一次（`Ctrl+Shift+B`）。WinForms 设计器需要先编译出自定义控件，才能正常渲染。

---

## 二、怎样查看窗口设计器

### WinForms（6 个窗体）

1. 在“解决方案资源管理器”里展开 `Answers.WinForms` → `Forms`；
2. 右键点击窗体文件（如 `AlarmFilterForm.cs`）→ **查看设计器**；
3. 也可以在打开 `.cs` 后按 `Shift+F7`（`F7` 在代码和设计器之间切换）。

能看到的内容：

- 所有 Label、Button、TextBox、DataGridView 的位置和大小；
- DataGridView 的每一列（列标题、宽度、`DataPropertyName`）；
- 窗体底部的“组件栏”：`Timer`、`NotifyIcon`、`ContextMenuStrip`，可以点选后改属性；
- `MainForm.Designer.cs` 是 6 个练习的总入口。

注意：`HoverTrendChart` 是自绘控件，设计器里只显示一个空白矩形，运行后才用 GDI+ 画曲线，这是正常的。

### WPF（5 个窗口 + 1 个入口窗口）

1. 展开 `Answers.Wpf` → `Windows`；
2. 双击任意 `.xaml`（如 `AlarmListWindow.xaml`），打开的就是设计视图；
3. `F7` / `Shift+F7` 在 XAML 与后台代码之间切换。

注意：设计时没有 ViewModel 实例，绑定值会显示为空白或占位值；运行后才由 ViewModel 填数据。窗口尺寸、颜色、间距、控件结构都能在设计器里直接看到和修改。

---

## 三、运行方式

### 在 Visual Studio / Rider 中

在解决方案资源管理器里右键项目 → **设为启动项目**，然后按 `F5`：

- 想看窗口：启动 `Answers.WinForms` 或 `Answers.Wpf`；
- 想看控制台输出：启动 `Answers.Console`。

### 命令行

```bat
cd /d D:\上位机\上位机学习大全\练习题答案

:: 控制台：跑全部 18 道题
dotnet run --project src\Answers.Console -- all

:: 控制台：只跑某一章
dotnet run --project src\Answers.Console -- 07

:: WinForms
dotnet run --project src\Answers.WinForms

:: WPF
dotnet run --project src\Answers.Wpf
```

---

## 四、目录结构

```
练习题答案/
├── 练习题答案.sln
├── 练习题答案.slnx
├── README.md
└── src/
    ├── Answers.Console/            控制台答案（18 道）
    │   ├── Chapter02/
    │   ├── Chapter07/
    │   ├── Chapter10/
    │   ├── Support/                自包含的 Modbus、轮询、模型代码
    │   ├── Chapter02.cs            章节菜单
    │   ├── Chapter07.cs
    │   └── Chapter10.cs
    │
    ├── Answers.WinForms/           WinForms 答案（6 个原生窗体）
    │   ├── Forms/
    │   │   ├── FirstTaskForm.cs / .Designer.cs
    │   │   ├── SendHistoryForm.cs / .Designer.cs
    │   │   ├── AlarmFilterForm.cs / .Designer.cs
    │   │   ├── TrendHoverForm.cs / .Designer.cs
    │   │   ├── VirtualGridForm.cs / .Designer.cs
    │   │   └── TrayForm.cs / .Designer.cs
    │   ├── Controls/HoverTrendChart.cs
    │   ├── Models/AlarmRow.cs
    │   ├── MainForm.cs / .Designer.cs
    │   ├── UiTheme.cs              颜色常量 + 表格统一样式
    │   └── Program.cs
    │
    └── Answers.Wpf/                WPF 答案（5 个独立 Window）
        ├── Windows/
        │   ├── AlarmSoundWindow.xaml / .xaml.cs
        │   ├── CrosshairWindow.xaml / .xaml.cs
        │   ├── DeviceCardsWindow.xaml / .xaml.cs
        │   ├── ThemeWindow.xaml / .xaml.cs
        │   └── AlarmListWindow.xaml / .xaml.cs
        ├── ViewModels/             MVVM 的 ViewModel 与命令
        ├── Models/
        ├── Controls/CrosshairTrendChart.cs
        ├── Selectors/DeviceTemplateSelector.cs
        ├── Services/ThemeManager.cs
        ├── Themes/                 Dark.xaml / Light.xaml / Styles.xaml
        ├── MainWindow.xaml / .xaml.cs
        └── App.xaml / .xaml.cs
```

---

## 五、练习索引

### WinForms

| 文件 | 对应练习 | 主要知识点 |
|---|---|---|
| `FirstTaskForm` | 第 01 章 · 第一个小任务 | TextBox + Button + Label + CheckBox、事件、回车触发 |
| `SendHistoryForm` | 第 04 章练习 1 · 发送历史 | 最近 10 条、JSON 持久化、双击回填 |
| `AlarmFilterForm` | 第 04 章练习 2 · 报警筛选 | `BindingList` + `DataGridView`、按状态筛选、自动追加 |
| `TrendHoverForm` | 第 04 章练习 3 · 曲线悬停 | GDI+ `OnPaint`、双缓冲、鼠标坐标反算、提示框 |
| `VirtualGridForm` | 第 04 章练习 4 · 虚拟表格 | `VirtualMode`、10 万行结构体、`CellValueNeeded` |
| `TrayForm` | 第 04 章练习 5 · 托盘最小化 | `NotifyIcon`、拦截 `FormClosing`、隐藏/恢复/真正退出 |

### WPF

| 文件 | 对应练习 | 主要知识点 |
|---|---|---|
| `AlarmSoundWindow` | 第 05 章练习 1 · 报警声音 | MVVM、`DispatcherTimer`、`SystemSounds`、命令 |
| `CrosshairWindow` | 第 05 章练习 2 · 趋势十字光标 | 依赖属性、`OnRender` 自绘、悬停十字线 |
| `DeviceCardsWindow` | 第 05 章练习 3 · 设备卡片 | `DataTemplateSelector`、`ItemsControl`、三种设备模板 |
| `ThemeWindow` | 第 05 章练习 4 · 日夜主题 | `ResourceDictionary`、`DynamicResource` 运行时换肤 |
| `AlarmListWindow` | 第 05 章练习 5 · 报警排序筛选 | `CollectionViewSource`、筛选、排序、`DataGrid` |

### Console（18 道）

| 章节 | 题号与内容 |
|---|---|
| 第 02 章 | 1 十六进制转 `byte[]`；2 `byte[]` 转十六进制；3 LINQ 筛选；4 JSON 序列化；5 事件；6 async/await；7 空引用异常 |
| 第 07 章 | 1 手发 Modbus 请求；2 解析读寄存器应答；3 手算 CRC；4 四种字序；5 100 ms 轮询；6 失败 3 次重连 |
| 第 10 章 | 1 定时器精度对比；2 `Channel` 解耦采集与存盘；3 `.Result` 死锁；4 连续失败阈值；5 关闭前全部落盘 |

---

## 六、这版和旧练习代码的区别

1. **WinForms 从“运行时 new 控件”改成真正的设计器代码**
   旧代码在构造函数里创建控件，设计器只看到空窗口。现在所有控件都在 `.Designer.cs` 的 `InitializeComponent()` 里创建，设计器能完整渲染；事件逻辑仍放在 `.cs` 里，两边职责清楚。

2. **WPF 从“一个主窗口 + UserControl 切换”改成“每题一个独立 Window”**
   每个窗口的完整布局都在自己的 XAML 中，双击即可预览；`MainWindow` 只作为 5 个窗口的启动页。颜色统一来自 `Themes`，换肤时所有已打开窗口一起变色。

3. **答案与主项目完全解耦**
   `Answers.Console` 自带 `Support` 下的 Modbus 组帧、CRC、寄存器解析、轮询代码，不引用 `Industrial.Core`；WinForms/WPF 也不引用三个主项目。复制代码是为了让答案能单独运行，主项目保持原样。

4. **修复了曲线控件的单点绘制问题**
   GDI+ 的 `DrawLines` 至少需要两个点。刚打开曲线窗时只有 1 个采样点，旧写法会抛异常并在控件上画红叉。现在只有一个点时先画点，第二个点到达后再画折线和面积。

---

## 七、验证结果

- 三个项目统一编译：**0 警告、0 错误**；
- 控制台 `-- all`：18 道题全部跑通（轮询、重连、`Channel`、落盘等结果均符合预期）；
- WinForms：6 个窗体全部能打开；曲线窗能正常画线并滚动；托盘窗点关闭后隐藏、进程保持，关闭主窗口后进程正常退出；
- WPF：5 个窗口全部能打开；日夜主题切换能同时改变主窗口和已打开窗口的颜色。

如果只想快速确认环境没问题，先运行：

```bat
dotnet build 练习题答案.sln
dotnet run --project src\Answers.Console -- all
```
