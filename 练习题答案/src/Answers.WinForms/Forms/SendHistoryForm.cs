using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Answers.WinForms.Forms;

/// <summary>
/// 第 04 章练习 1：给串口助手加“发送历史”（最近 10 条 + JSON 持久化）。
///
/// 现场调试经常重复发送同一条命令，所以串口助手都会保留下拉历史。
/// 本练习把历史存到用户目录，程序重启后仍然存在。
///
/// 接入真实串口助手时，只需要在“写串口成功”之后调用 AddToHistory；
/// 本练习用“发送（模拟）”按钮代替真正的串口写入。
/// </summary>
public partial class SendHistoryForm : Form
{
    private static readonly string HistoryFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "UpperComputerLearning", "Answers", "ch04-send-history.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly List<string> _history = new();

    public SendHistoryForm()
    {
        InitializeComponent();
        LoadHistory();
    }

    private void sendButton_Click(object? sender, EventArgs e) => SimulateSend();

    private void clearButton_Click(object? sender, EventArgs e) => ClearHistory();

    private void historyList_DoubleClick(object? sender, EventArgs e)
    {
        if (historyList.SelectedItem is not string text) return;

        historyCombo.Text = text;
        historyCombo.Focus();
        historyCombo.SelectionStart = historyCombo.Text.Length;
    }

    private void historyCombo_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (historyCombo.SelectedItem is string text) historyCombo.Text = text;
    }

    private void SendHistoryForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        // 每次发送/清空已经保存过；这里再存一次，保证用户手工改了下拉框内容也不丢。
        SaveHistory();
    }

    /// <summary>模拟发送：校验 → 去重 → 插到最前 → 截断 10 条 → 存盘 → 刷新。</summary>
    private void SimulateSend()
    {
        var text = historyCombo.Text.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            MessageBox.Show(this, "发送内容不能为空。", "提示",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // 去重后插到第一位：最近使用的排最上面，不会出现两条相同命令。
        _history.RemoveAll(item => string.Equals(item, text, StringComparison.Ordinal));
        _history.Insert(0, text);
        if (_history.Count > 10) _history.RemoveRange(10, _history.Count - 10);

        SaveHistory();
        RefreshViews();

        statusLabel.Text = $"已模拟发送：{text}（真实项目会在这里写串口并统计 TX 字节）";
        statusLabel.ForeColor = UiTheme.AccentDark;
        historyCombo.Focus();
        historyCombo.SelectionStart = historyCombo.Text.Length;
    }

    /// <summary>清空是破坏性操作，先确认。</summary>
    private void ClearHistory()
    {
        if (_history.Count == 0) return;

        if (MessageBox.Show(this, "确定清空最近 10 条发送历史？", "确认",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        _history.Clear();
        SaveHistory();
        RefreshViews();
        statusLabel.Text = "历史已清空。";
        statusLabel.ForeColor = UiTheme.Muted;
    }

    private void LoadHistory()
    {
        try
        {
            if (File.Exists(HistoryFile))
            {
                var json = File.ReadAllText(HistoryFile);
                var saved = JsonSerializer.Deserialize<List<string>>(json, JsonOptions);
                if (saved is not null)
                    _history.AddRange(saved.Where(s => !string.IsNullOrWhiteSpace(s)).Take(10));
            }
        }
        catch (Exception ex)
        {
            // 文件损坏、权限不足都不应该让程序崩溃，只在界面上提示。
            statusLabel.Text = $"读取历史失败：{ex.Message}";
            statusLabel.ForeColor = UiTheme.Alarm;
        }

        if (_history.Count == 0)
        {
            _history.AddRange(new[]
            {
                "01 03 00 00 00 0A C5 CD",
                "01 06 00 05 03 78 99 19",
                "AT+VER?"
            });
            statusLabel.Text = "首次运行：已放入 3 条示例，发送后会写入 JSON。";
        }

        RefreshViews();
    }

    private void SaveHistory()
    {
        try
        {
            var directory = Path.GetDirectoryName(HistoryFile)!;
            Directory.CreateDirectory(directory);

            // UTF-8 不带 BOM，避免部分工具读文件时把 BOM 当成内容。
            File.WriteAllText(HistoryFile,
                JsonSerializer.Serialize(_history, JsonOptions),
                new UTF8Encoding(false));

            fileLabel.Text = $"JSON: {HistoryFile}";
        }
        catch (Exception ex)
        {
            statusLabel.Text = $"保存历史失败：{ex.Message}";
            statusLabel.ForeColor = UiTheme.Alarm;
        }
    }

    /// <summary>把内存中的历史同步到下拉框和列表，并保留用户正在编辑的文字。</summary>
    private void RefreshViews()
    {
        historyCombo.BeginUpdate();
        var current = historyCombo.Text;
        historyCombo.Items.Clear();
        foreach (var item in _history) historyCombo.Items.Add(item);
        historyCombo.Text = current;
        historyCombo.EndUpdate();

        historyList.BeginUpdate();
        historyList.Items.Clear();
        foreach (var item in _history) historyList.Items.Add(item);
        historyList.EndUpdate();

        fileLabel.Text = $"JSON: {HistoryFile}";
    }
}
