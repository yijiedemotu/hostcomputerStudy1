namespace Exercises.WinForms.Views;

/// <summary>
/// 第 01 章第 8 节：TextBox + Button + Label + CheckBox 的 5 步小任务。
///
/// 题目要求：
/// 1. 输入文字，点按钮后显示大写；
/// 2. 输入为空时弹窗提示；
/// 3. 勾选 CheckBox 后结果变红；
/// 4. 取消勾选后恢复默认颜色；
/// 5. 按回车也能触发转换。
///
/// 这个页面虽然简单，但包含了 WinForms 最核心的套路：
///   控件（界面元素） + 事件（点击/按键） + 属性（Text/ForeColor） + 条件判断。
/// 后面所有复杂的上位机界面，都是这些基础元素组合出来的。
/// </summary>
internal sealed class FirstTaskPanel : UserControl
{
    // 把常用控件保存成字段，方法里才能访问它们。
    // readonly 表示“构造完之后不再换对象”，但控件内部的 Text 等属性仍可修改。
    private readonly TextBox _input;
    private readonly Label _result;
    private readonly CheckBox _red;

    public FirstTaskPanel()
    {
        // CreatePage 返回“标题 + 说明 + 内容区”的整体布局，
        // out var content 取出中间那块空白面板。
        var page = UiTheme.CreatePage(
            "第 01 章 · 第一个小任务",
            "输入文字 → 点按钮 → 显示大写；输入为空要提示；勾选 CheckBox 后结果变红。这就是控件、事件、字符串、条件判断和属性修改的最小组合。",
            out var content);
        Dock = DockStyle.Fill;
        Controls.Add(page);

        // FlowLayoutPanel：子控件按顺序自动排列，窗口变窄还能自动换行。
        // TopDown 表示从上往下排；WrapContents = false 表示不换行（这里主要靠 AutoScroll）。
        var stack = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = UiTheme.Surface
        };

        stack.Controls.Add(UiTheme.Label("1. 输入内容", UiTheme.SectionFont));

        // TextBox：文本输入框。Width 固定 560 像素，字号调大方便演示。
        _input = new TextBox
        {
            Width = 560,
            Font = new Font("Microsoft YaHei UI", 12F),
            Margin = new Padding(0, 0, 0, 14)
        };
        stack.Controls.Add(_input);

        // CheckBox：复选框。CheckedChanged 在“勾选状态改变”时触发。
        // 这里用 Lambda 表达式 (_, _) => ApplyColor()，表示事件发生时调用 ApplyColor。
        _red = new CheckBox
        {
            Text = "勾选后，结果显示为红色",
            AutoSize = true,
            Font = UiTheme.BodyFont,
            ForeColor = UiTheme.Text,
            Margin = new Padding(0, 0, 0, 14)
        };
        _red.CheckedChanged += (_, _) => ApplyColor();
        stack.Controls.Add(_red);

        // Button：按钮。Click 事件在“点击后”触发。
        var send = UiTheme.Button("转成大写", primary: true);
        send.Click += (_, _) => ConvertText();
        stack.Controls.Add(send);

        stack.Controls.Add(UiTheme.Label("2. 结果", UiTheme.SectionFont));

        // Label：只读文字。这里用来显示转换结果。
        _result = new Label
        {
            Text = "结果会显示在这里",
            Font = UiTheme.ResultFont,
            ForeColor = UiTheme.Muted,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 18)
        };
        stack.Controls.Add(_result);

        stack.Controls.Add(UiTheme.Hint("对应文档 01 第 8 节。验收点：输入为空弹窗“请输入内容”；大写转换正确；颜色随 CheckBox 变化。"));
        content.Controls.Add(stack);

        // KeyDown：键盘按下时触发。
        // 判断是不是 Enter 键；是的话执行转换，并告诉系统“这个键我处理了，不要再发出提示音”。
        _input.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            ConvertText();
        };
    }

    /// <summary>读取输入 → 校验 → 转大写 → 刷新颜色。按钮和回车都调用它。</summary>
    private void ConvertText()
    {
        var text = _input.Text;

        // IsNullOrWhiteSpace：null、空字符串、只含空格，都算“没输入”。
        // 这是上位机参数输入框最常用的校验之一。
        if (string.IsNullOrWhiteSpace(text))
        {
            // MessageBox.Show(父窗口, 内容, 标题, 按钮, 图标)
            // 传 this 可以让弹窗依附在当前页面/窗体上，居中显示。
            MessageBox.Show(this, "请输入内容", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _input.Focus();   // 光标回到输入框，方便用户直接改正
            return;           // 校验失败，直接结束，不执行后面的转换
        }

        // ToUpperInvariant 按“与语言无关”的规则转大写。
        // 相比 ToUpper()，它不受当前系统区域设置影响，适合处理协议/代码类文本。
        _result.Text = text.ToUpperInvariant();
        ApplyColor();
    }

    /// <summary>根据 CheckBox 的状态设置结果文字颜色。</summary>
    private void ApplyColor()
    {
        // 结果还是占位文字时不要染色，避免一打开就是红色。
        if (_result.Text == "结果会显示在这里") return;

        // 三目运算符：勾选 → 红色，未勾选 → 主题正文色。
        _result.ForeColor = _red.Checked ? Color.Red : UiTheme.Text;
    }

    /// <summary>
    /// 设计器自动生成的占位方法。本项目界面全部用代码创建，不需要它，
    /// 保留是为了兼容 Visual Studio 设计器打开 UserControl 的行为。
    /// </summary>
    private void InitializeComponent()
    {
        SuspendLayout();
        // 
        // FirstTaskPanel
        // 
        Size = new System.Drawing.Size(465, 422);
        ResumeLayout(false);
    }
}
