# 01 · WinForms 布局原理与最小窗口（里程碑 M1）

> **本章目标**：搞懂 Dock 布局，敲出一个"上工具条 / 下状态栏 / 中间左右分栏 / 左栏再上下分栏"的窗口。
> **验收**：拖大拖小时四块区域始终填满、不重叠；拖动分隔条能调整比例；把窗口缩到最小也不塌。
>
> ⚠️ 这一章是**整个教程最关键的一章**：第 02 章的完整界面完全建立在这几个规则上。
> 本项目第一版就是因为没搞懂这些，界面错乱成一团（复盘见 [07 章](07-调试排错与真实踩坑复盘.md)）。

---

## 1. WinForms 三种定位方式

| 方式 | 属性 | 行为 | 适合 |
|---|---|---|---|
| 绝对定位 | `Location` + `Size` | 写死像素坐标，窗口缩放时不动 | 固定对话框、小工具 |
| 锚定 | `Anchor = Top \| Left \| Right \| Bottom` | 边缘"粘"住父容器，跟随拉伸 | 单个控件跟着窗口变 |
| **停靠** | `Dock = Top / Bottom / Left / Right / Fill` | 贴住父容器某条边，`Fill` 吃掉剩余空间 | **主界面分区**（本教程用它） |

我们只用 Dock，因为监控窗口、终端面板都要求"随窗口缩放自动铺满"。

---

## 2. Dock 的头号规则：添加顺序（z-order）

**WinForms 按 z-order 的反向顺序处理停靠**：`Controls` 集合里**后添加**的控件先贴边，**索引 0**（最先添加）的最后处理、吃掉剩余空间。

所以：

```csharp
Controls.Add(fillControl);   // ① Fill 必须第一个 Add
Controls.Add(bottomControl); // ② Bottom
Controls.Add(topControl);    // ③ Top
```

如果反过来（Fill 最后 Add），Top/Bottom 还没占边时 Fill 先把整个客户区吃了，
后面的边缘控件会叠在它上面或被挤到 0 高 —— 界面就"乱"了。

> 记忆口诀：**「Fill 先加，边缘后加」**。
> VS 设计器生成的 `.Designer.cs` 也是这个顺序，只是它帮你排好了。

### 嵌套容器里同样适用

右侧面板里"监控 Fill + 帮助 Bottom"：

```csharp
split.Panel2.Controls.Add(gbMonitor); // Fill 先
split.Panel2.Controls.Add(gbHelp);     // Bottom 后
```

终端面板里"接收区 Fill + 发送行 Bottom + 选项条 Top"：

```csharp
Controls.Add(_recv);     // Fill
Controls.Add(sendRow);   // Bottom
Controls.Add(bar);       // Top
```

---

## 3. 第二个规则：`SplitterPanel` 不会自动 Dock

`SplitContainer.Panel1` / `Panel2` 是普通 `Panel`，往里 `Controls.Add(x)` **不会**自动把 `x` 设成 `Dock=Fill`。
`GroupBox` 默认尺寸是 200×100，不显式设置就会保持这个大小，内部控件被压扁。

```csharp
var t1 = new TerminalPanel(...);
t1.Dock = DockStyle.Fill;    // ← 必须显式写这一行！
leftSplit.Panel1.Controls.Add(t1);
```

> 对比：`TableLayoutPanel` 的单元格会自动把子控件撑满，所以从 TableLayoutPanel 换到 SplitContainer 时最容易漏掉这行。

---

## 4. 第三个规则：不要在构造函数里设 `PanelMinSize`

```csharp
// ❌ 错误示范：会抛异常
var split = new SplitContainer {
    Dock = DockStyle.Fill,
    Panel1MinSize = 380,   // 此时 split.Width 还是默认值 ~150
    Panel2MinSize = 300    // 赋这行时立刻校验 SplitterDistance → InvalidOperationException
};
```

异常原文：

```
System.InvalidOperationException: SplitterDistance 必须在 Panel1MinSize 和 Width - Panel2MinSize 之间。
   at System.Windows.Forms.SplitContainer.set_SplitterDistance(Int32 value)
   at System.Windows.Forms.SplitContainer.ApplyPanel2MinSize(Int32 value)
```

原因：设置 `Panel2MinSize` 时控件还没有实际尺寸，SplitContainer 会立即验证当前 `SplitterDistance` 是否合法，而默认值超出了 `Width - Panel2MinSize`。

**正确做法**：MinSize 用默认值（25），分栏位置等窗口 `Shown`（此时尺寸已确定）再设：

```csharp
Shown += (_, _) =>
{
    var d = (int)(split.Width * 0.58);
    d = Math.Clamp(d, 60, Math.Max(60, split.Width - 60));   // 留足合法余量
    split.SplitterDistance = d;
};
```

---

## 5. 为什么本教程**不用** `TableLayoutPanel`

`TableLayoutPanel` 很好用，但有个隐蔽陷阱：

> 单列容器如果**没有**显式添加 `ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100))`，
> 该列默认是 `AutoSize`，列宽按"内容首选宽度"计算，
> 子控件即使 `Dock=Fill` 也只有那么宽 → 全部挤到左上角。

嵌套三层时只要漏一层，整个界面就塌缩。本项目最初就是这么翻车的（用户截图里两个终端只剩几行标题文字）。
用 Dock + SplitContainer 没有"行列样式"这个隐藏状态，更不容易出错，所以本教程全程用它。

---

## 6. M1 代码（`MainForm.cs` 整体替换）

把第 00 章的 `MainForm.cs` **整体替换**为下面这份：

```csharp
using System.Drawing;
using System.Windows.Forms;

namespace VComPair;

public sealed class MainForm : Form
{
    public MainForm()
    {
        Text = "VComPair · M1 布局演示";
        Size = new Size(900, 560);
        MinimumSize = new Size(640, 420);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Microsoft YaHei UI", 9F);
        BuildUi();
    }

    private void BuildUi()
    {
        // ---------- 顶部工具条（Top） ----------
        var top = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 46,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(4)
        };
        top.Controls.Add(new Button { Text = "① 启动服务", Width = 120, Height = 28 });
        top.Controls.Add(new Button { Text = "停止服务", Width = 100, Height = 28 });
        top.Controls.Add(new Label { Text = "端口", AutoSize = true, Margin = new Padding(10, 8, 4, 0) });
        top.Controls.Add(new TextBox { Text = "9527", Width = 68 });

        // ---------- 底部状态栏（Bottom） ----------
        var status = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 26,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "状态栏：VCOM1 空闲 | VCOM2 空闲",
            ForeColor = Color.DimGray,
            Padding = new Padding(6, 0, 6, 0)
        };

        // ---------- 中间：左右 SplitContainer（Fill） ----------
        var mainSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 6
        };

        // 左栏内部再上下对半分
        var leftSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterWidth = 6
        };
        leftSplit.Panel1.Controls.Add(MakeBlock("左上 · VCOM1 终端", Color.AliceBlue));
        leftSplit.Panel2.Controls.Add(MakeBlock("左下 · VCOM2 终端", Color.Honeydew));
        mainSplit.Panel1.Controls.Add(leftSplit);
        mainSplit.Panel2.Controls.Add(MakeBlock("右侧 · 数据监控", Color.LightYellow));

        // ---------- 关键：Fill 先 Add，边缘后 Add ----------
        Controls.Add(mainSplit); // Fill
        Controls.Add(status);   // Bottom
        Controls.Add(top);      // Top

        // 分栏比例等窗口显示、尺寸确定后再设
        Shown += (_, _) =>
        {
            try
            {
                var d = (int)(mainSplit.Width * 0.58);
                d = Math.Clamp(d, 60, Math.Max(60, mainSplit.Width - 60));
                mainSplit.SplitterDistance = d;

                var h = leftSplit.Height / 2;
                h = Math.Clamp(h, 60, Math.Max(60, leftSplit.Height - 60));
                leftSplit.SplitterDistance = h;
            }
            catch { /* 极端尺寸下保持默认分栏 */ }
        };
    }

    /// <summary>造一块带标题的彩色占位面板，用来直观看到分区。</summary>
    private static Panel MakeBlock(string title, Color color)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,   // 放进 SplitterPanel 必须显式 Fill
            BackColor = color,
            Padding = new Padding(8)
        };
        panel.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Text = title,
            Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold)
        });
        return panel;
    }
}
```

---

## 7. 逐段讲解

### 7.1 `FlowLayoutPanel` 顶部工具条

`FlowLayoutPanel` 会像文字一样从左到右排列子控件：
- `WrapContents = false`：不换行（窄窗口时配合 `AutoScroll=true` 出现水平滚动条兜底）；
- `Margin`：控件之间的间距，比手工算 `Location` 省事得多；
- `Padding`：容器四周留白。

### 7.2 两个 SplitContainer 嵌套

```
mainSplit (Vertical → 左右)
├─ Panel1 → leftSplit (Horizontal → 上下)
│            ├─ Panel1  VCOM1
│            └─ Panel2  VCOM2
└─ Panel2 → 数据监控
```

`Orientation.Vertical` 表示**分隔条是竖的**（分成左右）；`Horizontal` 表示分隔条是横的（分成上下）。容易记反，注意。

### 7.3 `Shown` 事件

`Load` 事件时控件尺寸可能还没最终确定；`Shown` 是窗口**第一次真正显示出来**之后触发，此时 `Width/Height` 可靠，适合设 `SplitterDistance`。

### 7.4 `Math.Clamp`

`Math.Clamp(value, min, max)` 把值夹在区间内。`max` 用 `Math.Max(60, Width - 60)` 是为了窗口极小时 `max >= min`，避免 `Clamp` 抛 `ArgumentException`。

---

## 8. 动手实验（强烈建议做一遍）

| 实验 | 怎么改 | 你会看到什么 | 结论 |
|---|---|---|---|
| A | 把 `Controls.Add(mainSplit);` 移到三个 Add 的**最后** | 工具条/状态栏被 Fill 面板盖住或挤没 | 验证 z-order 规则 |
| B | 删掉 `MakeBlock` 里的 `Dock = DockStyle.Fill` | 彩色块变成默认 200×100 缩在角落 | 验证 SplitterPanel 不自动 Dock |
| C | 在 `mainSplit` 初始化里加回 `Panel2MinSize = 300` | 启动时直接抛 `InvalidOperationException` | 验证第 4 节那个坑 |
| D | 把窗口拖到最小再拖回最大 | 分区始终填满、分隔条保持可用 | Dock 布局的伸缩性 |

> 做完实验记得**改回正确版本**再进入第 02 章。

---

## 9. 验收清单

- [ ] `Ctrl+Shift+B` 0 错误 0 警告，`F5` 出现四块彩色分区
- [ ] 拖大/拖小/最大化，四块始终填满，不重叠、不塌缩
- [ ] 能拖动两条分隔条调整比例
- [ ] 缩到 `MinimumSize` 时顶部按钮仍完整可见
- [ ] 能口头复述「Fill 先加、边缘后加」和「SplitterPanel 要显式 Dock=Fill」
- [ ] 能说清为什么 `PanelMinSize` 不能在构造函数里设

---

**下一章**：[02 · 完整界面搭建](02-完整界面搭建.md) —— 把彩色占位块换成真正的控件，敲出成品界面。
