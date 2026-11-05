namespace ModbusMonitor;

/// <summary>一个小输入框（写入设定值用）。WinForms 没有内置 InputBox，自己写一个更可控。</summary>
internal sealed class InputDialog : Form
{
    private readonly TextBox _textBox;

    /// <summary>
    /// 构造函数：全部用代码创建控件，不依赖设计器。
    /// Dock 布局：提示文字在顶部、按钮栏在底部、输入框填满中间。
    /// </summary>
    private InputDialog(string title, string prompt, string defaultValue)
    {
        Text = title;
        FormBorderStyle = FormBorderStyle.FixedDialog; // 固定大小，不允许拖拽缩放
        StartPosition = FormStartPosition.CenterParent; // 居中显示在父窗口上
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false; // 小对话框不占任务栏
        ClientSize = new Size(380, 150);

        var label = new Label
        {
            Text = prompt,
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 52,
            Padding = new Padding(12, 12, 12, 0)
        };

        _textBox = new TextBox
        {
            Text = defaultValue,
            Dock = DockStyle.Top,
            Margin = new Padding(12)
        };

        // 底部按钮栏
        var panel = new Panel { Dock = DockStyle.Bottom, Height = 46, Padding = new Padding(12, 8, 12, 8) };

        // DialogResult 属性让按钮自动关闭窗口，并把结果返回给 ShowDialog
        var ok = new Button { Text = "确定", DialogResult = DialogResult.OK, Dock = DockStyle.Right, Width = 88 };
        var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Dock = DockStyle.Right, Width = 88 };
        panel.Controls.Add(ok);
        panel.Controls.Add(cancel);

        // 中间区域放输入框
        var host = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 6, 12, 0) };
        host.Controls.Add(_textBox);

        // Dock 的顺序同样重要：后加的先占位，所以 Fill 的 host 要最先加
        Controls.Add(host);
        Controls.Add(panel);
        Controls.Add(label);

        AcceptButton = ok;  // 按回车 = 点确定
        CancelButton = cancel; // 按 Esc = 点取消
    }

    /// <summary>
    /// 显示对话框。确定返回输入内容，取消返回 null。
    /// 用 static 方法包一层，调用方不需要关心窗体生命周期。
    /// </summary>
    public static string? Show(IWin32Window owner, string title, string prompt, string defaultValue = "")
    {
        using var dialog = new InputDialog(title, prompt, defaultValue);
        return dialog.ShowDialog(owner) == DialogResult.OK ? dialog._textBox.Text.Trim() : null;
    }
}
