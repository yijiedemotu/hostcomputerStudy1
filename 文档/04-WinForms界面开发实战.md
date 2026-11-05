# 04 WinForms 界面开发实战

> 本章目标：能独立做出工业风格的 WinForms 界面，并解决“卡顿、闪烁、跨线程”三大经典问题。
> 参考实现：`项目/src/SerialAssistant` 和 `项目/src/ModbusMonitor`。

---

## 1. 为什么上位机还在用 WinForms

| 维度 | WinForms | WPF |
|---|---|---|
| 上手速度 | 极快，拖控件即可 | 需要学 XAML/绑定 |
| 存量项目 | 非常多（10 年以上老项目） | 新项目逐渐增多 |
| 高 DPI | 一般（需额外配置） | 好 |
| 自定义外观 | 一般（自绘较麻烦） | 强（样式/模板） |
| 大表格性能 | 好（虚拟模式） | 一般 |
| 招人要求 | “会 WinForms 即可” | “熟悉 MVVM 优先” |

**结论：两个都要会。** 先用 WinForms 建立“界面 + 事件 + 线程”的直觉，再学 WPF 的 MVVM。

---

## 2. 控件速查表

| 控件 | 用途 | 上位机典型场景 |
|---|---|---|
| `Label` | 文本显示 | 标题、单位、状态 |
| `TextBox` | 输入/显示 | 参数、IP、发送内容 |
| `Button` | 按钮 | 启停、下发、清空 |
| `CheckBox` / `RadioButton` | 开关/单选 | HEX 显示、模式选择 |
| `ComboBox` | 下拉 | 串口号、波特率、设备列表 |
| `NumericUpDown` | 数值输入 | 设定值、间隔时间 |
| `GroupBox` / `Panel` | 分组/容器 | 参数区、状态区 |
| `TableLayoutPanel` | 表格布局 | 参数表单 |
| `FlowLayoutPanel` | 流式布局 | 按钮组、卡片 |
| `SplitContainer` | 可拖动分栏 | 左表格右曲线 |
| `TabControl` | 多页 | 实时/报警/日志 |
| `DataGridView` | 数据表格 | 点位表、报警表、历史查询 |
| `ListView` / `TreeView` | 列表/树 | 设备树、文件列表 |
| `StatusStrip` | 状态栏 | 连接状态、收发计数 |
| `ToolStrip` | 工具栏 | 启动/停止/设置 |
| `Timer` | 定时器 | 刷新界面、定时发送 |
| `PictureBox` | 图片 | 设备示意图、Logo |
| `ContextMenuStrip` | 右键菜单 | 写值、复制 |
| `Chart`（第三方） | 曲线 | 趋势图（本仓库手写） |

---

## 3. 布局：三条铁律

### 3.1 Dock（贴边）

```csharp
toolStrip.Dock = DockStyle.Top;
statusStrip.Dock = DockStyle.Bottom;
splitContainer.Dock = DockStyle.Fill;   // 最后填满剩余空间
```

**添加顺序影响结果**：先加 Fill，再加 Top/Bottom（Designer 生成的顺序就是反的）。
本仓库 `MainForm.Designer.cs` 里：

```csharp
Controls.Add(splitMain);      // Fill
Controls.Add(statusStrip);    // Bottom
Controls.Add(panelOptions);   // Top
Controls.Add(panelParams);    // Top（最上面）
```

### 3.2 Anchor（跟随缩放）

```csharp
button.Anchor = AnchorStyles.Top | AnchorStyles.Right;   // 右上角固定
textBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; // 随宽度拉伸
```

### 3.3 分辨率适配

现场屏幕可能是 1366×768，也可能是 1920×1080 + 125% 缩放。

- 窗体设 `MinimumSize`，防止拖太小把控件挤没；
- 用 `TableLayoutPanel` 的百分比列，不要全用绝对坐标；
- `AutoScaleMode = AutoScaleMode.Font`（默认）；
- 高 DPI 在 `.csproj` 加 `<ApplicationHighDpiMode>PerMonitorV2</ApplicationHighDpiMode>`；
- 字体用 `Microsoft YaHei UI` 9~10pt，别用固定像素。

---

## 4. 事件模型

```csharp
button.Click += Button_Click;

private void Button_Click(object? sender, EventArgs e)
{
    // 事件处理器：拿 sender 判断是谁触发的
}
```

常用事件：

| 事件 | 触发时机 | 用途 |
|---|---|---|
| `Click` | 点击 | 按钮 |
| `CheckedChanged` | 勾选变化 | 开关 |
| `SelectedIndexChanged` | 下拉选择变化 | 切换设备 |
| `TextChanged` | 文本变化 | 实时校验 |
| `FormClosing` | 关闭前 | 停采集、存配置 |
| `Load` | 窗体加载 | 初始化 |
| `CellFormatting` | 单元格绘制前 | 按状态染色 |
| `SelectionChanged` | 选中行变化 | 切换曲线 |

**窗体关闭必须清理资源**：

```csharp
private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
{
    _closing = true;
    tmrFlush.Stop();
    _service?.StopAsync().Wait(1500);
    FlushHistory();
}
```

---

## 5. 数据绑定：别一行行改单元格

### 5.1 BindingList + INotifyPropertyChanged

`INotifyPropertyChanged`：.NET 内置**属性变更通知接口**，是 UI 绑定自动刷新的核心。

> 没有这个接口：你改了 `Value`，后台变量变了，**表格界面不会自己更新**。

```csharp
public sealed class TagRow : INotifyPropertyChanged
{
    private string _value = "--";//后端存值，默认显示 `--`（没读到数据时的占位）
    public string Value
    {
        get => _value;
        set { _value = value; PropertyChanged?.Invoke(this, new(nameof(Value))); }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
}
//- 
  1. **触发 `PropertyChanged` 事件**，告诉绑定系统：`Value` 这个属性变了！UI 赶紧更新
- `?.Invoke`：空安全，如果没人订阅这个事件（没绑定 UI）就不执行，避免报错
- `nameof(Value)`：编译期拿到属性名字符串 `"Value"`，比直接写字符串更稳，重构改名不会炸

// 绑定
var rows = new BindingList<TagRow>();
grid.DataSource = rows;
```

后台只需要：

```csharp
row.Value = "86.5";   // 界面自动刷新
```

### 5.2 列头中文 + 自动列

```csharp
[DisplayName("位号")] public string Tag { get; }
```

设置 `AutoGenerateColumns = true`，列头直接显示中文，不用手写列定义。

### 5.3 按状态染色

```csharp
private void gridTags_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
{
    if (gridTags.Rows[e.RowIndex].DataBoundItem is not TagRow row) return;
    if (row.IsBad)
    {
        e.CellStyle.BackColor = Color.FromArgb(38, 38, 42);
        e.CellStyle.ForeColor = Color.Gray;
    }
    else if (row.IsAlarm)
    {
        e.CellStyle.BackColor = Color.FromArgb(74, 32, 32);
        e.CellStyle.ForeColor = Color.OrangeRed;
    }
}
```

### 5.4 大数据量

超过 1 万行不要用 `DataSource`，改用 `VirtualMode`：

```csharp
grid.VirtualMode = true;
grid.CellValueNeeded += (s, e) => e.Value = _cache[e.RowIndex].Value;
```

---

## 6. 跨线程更新界面（最高频面试题）

WinForms 控件只能在创建它的线程（UI 线程）上访问。后台线程直接改控件会抛：

> `InvalidOperationException: 线程间操作无效：从不是创建控件的线程访问它。`

三种正确姿势：

### 6.1 Invoke / BeginInvoke

```csharp
if (InvokeRequired)
    BeginInvoke(new Action(() => label.Text = "已连接"));
else
    label.Text = "已连接";
```

- `Invoke`：同步，等 UI 执行完返回；
- `BeginInvoke`：异步，投递到消息队列立即返回（**采集线程必须用这个，否则会互相等死**）。

本仓库 `ModbusMonitor/MainForm.cs`：

```csharp
private void OnSnapshotReady(IReadOnlyList<TagValue> values)
{
    if (_closing || IsDisposed) return;
    try { BeginInvoke(new Action<IReadOnlyList<TagValue>>(ApplySnapshot), values); }
    catch (ObjectDisposedException) { }
    catch (InvalidOperationException) { }   // 窗口已销毁
}
```

### 6.2 SynchronizationContext（WPF 项目里用）

```csharp
private readonly SynchronizationContext _ui = SynchronizationContext.Current!;
_ui.Post(_ => Apply(values), null);
```

### 6.3 async/await（推荐）

```csharp
private async void btnRead_Click(object sender, EventArgs e)
{
    var values = await master.ReadHoldingRegistersAsync(1, 0, 10);
    label.Text = values[0].ToString();   // await 之后自动回到 UI 线程
}
```

> 注意：`await` 默认捕获上下文，所以能回来；库代码里加 `ConfigureAwait(false)` 则不会。

---

## 7. 自定义控件：手写趋势图

`ModbusMonitor/TrendChart.cs` 的做法：

```csharp
public sealed class TrendChart : Control
{
    public TrendChart()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint |
                 ControlStyles.ResizeRedraw, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        // 1. 背景 2. 网格 3. 坐标刻度 4. 报警线 5. 折线 6. 当前值
    }
}
```

要点：

1. **双缓冲**消除闪烁（`OptimizedDoubleBuffer`）。
2. **限制点数**：只保留最近 1500 点，否则越画越慢。
3. **溢出裁剪**：`g.SetClip(plot)`，防止线画到坐标轴外。
4. **按需重绘**：数据来了才 `Invalidate()`，不要用 Timer 每 16ms 重画。

---

## 8. 界面美化（不花哨但要专业）

| 项 | 建议 |
|---|---|
| 主题 | 深色（`#171A21` 背景 + `#00DCA0` 强调色），工业风 |
| 字体 | Microsoft YaHei UI，正文 9~10pt，数值大号加粗 |
| 间距 | 统一 8/12/16，不要东一个 7 西一个 13 |
| 颜色 | 正常绿、报警红、故障灰；不要超过 5 种主色 |
| 图标 | 用 emoji 或内置图标，别用来源不明的 ico |
| 表格 | 隔行变色、行高 28~30、去掉行头 |
| 状态 | 顶部或底部固定状态栏，永远能看到“连没连上” |

---

## 9. 三个经典问题排查

### 9.1 界面卡顿

```
现象：点按钮后整个窗口“未响应”
原因：UI 线程里做了同步 IO / Thread.Sleep / 大量计算
定位：调试 → 暂停 → 看调用堆栈停在哪个方法
修复：改成 async/await 或放到后台线程
```

### 9.2 界面闪烁

```
原因：控件重绘时先擦背景再画内容
修复：双缓冲 + 减少刷新频率 + 只刷新变化区域
```

### 9.3 内存增长

```
原因：事件未退订、List 只增不减、每次刷新 new 大对象
修复：Dispose 里退订、限制集合长度、复用缓冲区
```

---

## 10. 本项目代码导读

### 串口助手 `SerialAssistant`

| 文件 | 内容 |
|---|---|
| `Program.cs` | 入口、全局异常、注册 GB2312 编码 |
| `MainForm.Designer.cs` | 全部界面布局（参数区/接收区/发送区/状态栏） |
| `MainForm.cs` | 打开串口、后台接收循环、HEX 解析、定时发送 |

关键代码：后台接收 + `BeginInvoke`：

```csharp
_receiveTask = Task.Run(() =>
{
    var buffer = new byte[8192];
    while (!_closing && _port is not null)
    {
        var n = port.Read(buffer, 0, buffer.Length, 50);
        if (n <= 0) continue;
        BeginInvoke(new Action<byte[]>(AppendReceived), chunk);
    }
});
```

### 监控上位机 `ModbusMonitor`

| 文件 | 内容 |
|---|---|
| `MainForm.Designer.cs` | 工具栏/表格/曲线/报警/日志 |
| `MainForm.cs` | 配置加载、采集启停、报警判定、CSV 落盘、写寄存器 |
| `TrendChart.cs` | 手写 GDI+ 趋势图 |
| `ViewModels.cs` | `TagRow`/`AlarmRecord` 数据绑定模型 |
| `InputDialog.cs` | 写值输入框 |

---

## 11. 练习

1. 给串口助手加“发送历史”（下拉框记录最近 10 条），用 `Settings` 或 JSON 持久化。
2. 给表格加“按报警状态筛选”的按钮。
3. 给趋势图加鼠标悬停显示数值。
4. 把 DataGridView 换成 `VirtualMode`，加载 10 万行历史数据测试滚动流畅度。
5. 实现“窗口关闭时最小化到托盘”，托盘双击恢复。

> 完整练习答案：`练习题答案/src/Answers.WinForms/Forms/`，5 道练习分别对应 `SendHistoryForm`、`AlarmFilterForm`、`TrendHoverForm`、`VirtualGridForm`、`TrayForm`；启动页还额外带第 01 章的 `FirstTaskForm`。
> 运行 `dotnet run --project 练习题答案\src\Answers.WinForms`，在启动页选择练习。
> 每个窗体都有同名 `.Designer.cs`：先编译一次，再右键 `.cs` → 查看设计器，能看到完整布局、表格列，以及组件栏里的 `Timer` / `NotifyIcon`。
> 自绘的 `HoverTrendChart` 在设计器里显示为空白矩形，运行时才画曲线，这是正常的。
> 答案为了不修改主项目，用独立窗体演示；把窗体和事件逻辑搬回 `SerialAssistant`，就是实际项目里的做法。

---

## 12. 本章验收

- [ ] 能独立用 Dock/Anchor 搭出三栏布局
- [ ] 能用 `BindingList<INotifyPropertyChanged>` 更新表格
- [ ] 能解释 `Invoke` 与 `BeginInvoke` 的区别
- [ ] 能写出双缓冲自绘控件
- [ ] 能定位一次“界面未响应”的调用堆栈
- [ ] 完成第 11 节至少 2 个练习

下一章：`05 WPF 与 MVVM.md`
