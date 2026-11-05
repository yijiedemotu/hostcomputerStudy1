namespace Answers.WinForms.Forms;

/// <summary>
/// 第 01 章第 8 节：TextBox + Button + Label + CheckBox 的 5 步小任务。
///
/// 题目要求：
/// 1. 输入文字，点按钮显示大写；
/// 2. 输入为空时弹窗提示；
/// 3. 勾选 CheckBox 后结果变红；
/// 4. 取消勾选恢复默认颜色；
/// 5. 按回车也能触发转换。
///
/// 界面全部在 FirstTaskForm.Designer.cs 中定义，本文件只写事件逻辑。
/// </summary>
public partial class FirstTaskForm : Form
{
    private const string Placeholder = "结果会显示在这里";

    public FirstTaskForm()
    {
        InitializeComponent();

        // 打开窗体后把光标放进输入框，用户可以直接打字。
        Shown += (_, _) => inputBox.Focus();
    }

    private void convertButton_Click(object? sender, EventArgs e) => ConvertText();

    private void inputBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter) return;

        // 回车已经用来触发转换，不要再让系统发出“叮”的提示音。
        e.SuppressKeyPress = true;
        ConvertText();
    }

    private void redCheck_CheckedChanged(object? sender, EventArgs e) => ApplyColor();

    /// <summary>读取 → 校验 → 转大写 → 刷新颜色。按钮和回车共用。</summary>
    private void ConvertText()
    {
        var text = inputBox.Text;

        // null、空字符串、只有空格，都算没输入。
        if (string.IsNullOrWhiteSpace(text))
        {
            MessageBox.Show(this, "请输入内容", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            inputBox.Focus();
            return;
        }

        // ToUpperInvariant 不受系统区域设置影响，适合协议/代码类文本。
        resultLabel.Text = text.ToUpperInvariant();
        ApplyColor();
    }

    /// <summary>勾选时红色，否则恢复主题正文色；占位文字不染色。</summary>
    private void ApplyColor()
    {
        if (resultLabel.Text == Placeholder) return;
        resultLabel.ForeColor = redCheck.Checked ? Color.Red : UiTheme.Text;
    }
}
