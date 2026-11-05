using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Exercises.WinForms.Views;

/// <summary>
/// 第 04 章练习 1：给串口助手加“发送历史”（最近 10 条 + JSON 持久化）。
///
/// 要解决的问题：
/// 现场调试时经常重复发送同一条命令。每次都手打太麻烦，
/// 所以串口助手都会保留下拉历史；程序重启后历史还在，就需要存到文件。
///
/// 本练习的做法：
/// - _history 保存最近 10 条文本，最新的在最前面；
/// - 每次发送后去重、插到第一位、截断到 10 条；
/// - 序列化成 JSON 写到本机用户目录；
/// - 启动时读回 JSON，填进 ComboBox 和 ListBox。
///
/// 真项目里的接入点：SendCurrent() 成功写串口后调用 AddToHistory(text)；
/// 主窗体 FormClosing 时 SaveHistory()。
/// </summary>
internal sealed class SendHistoryPanel : UserControl
{
    // 历史文件放在“当前用户的本地应用数据”目录，例如：
    // C:\Users\你的用户名\AppData\Local\UpperComputerLearning\ch04-send-history.json
    // 这样不会污染程序安装目录，也不会因为程序没有写权限而失败。
    private static readonly string HistoryFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "UpperComputerLearning", "ch04-send-history.json");

    // JSON 序列化选项：
    // WriteIndented = true 让文件带缩进，方便人直接打开看；
    // UnsafeRelaxedJsonEscaping 让中文原样保存，不转成 \uXXXX。
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly ComboBox _combo;      // 可输入、可选择的下拉框
    private readonly ListBox _list;        // 历史列表，双击可回填
    private readonly Label _status;        // 状态提示
    private readonly Label _fileLabel;     // 显示 JSON 文件路径
    private readonly List<string> _history = new();   // 内存中的最近 10 条

    public SendHistoryPanel()
    {
        var page = UiTheme.CreatePage(
            "第 04 章练习 1 · 发送历史",
            "下拉框记录最近 10 条发送内容，并用 JSON 持久化；重启程序后历史仍在。真实串口助手里，在发送成功后调用 AddToHistory 即可。",
            out var content);
        Dock = DockStyle.Fill;
        Controls.Add(page);

        // 三行布局：工具栏、历史列表、底部状态。
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = UiTheme.Surface
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // ---------- 第一行：输入下拉框 + 发送 + 清空 ----------
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true,
            Margin = new Padding(0, 0, 0, 10)
        };

        _combo = new ComboBox
        {
            Width = 540,
            // DropDown 风格：既能从列表里选，也能直接输入新的命令。
            DropDownStyle = ComboBoxStyle.DropDown,
            Font = UiTheme.MonoFont,   // 等宽字体，十六进制帧看起来更整齐
            Margin = new Padding(0, 0, 10, 0)
        };

        var send = UiTheme.Button("发送（模拟）", primary: true);
        send.Click += (_, _) => SimulateSend();

        var clear = UiTheme.Button("清空历史");
        clear.Click += (_, _) => ClearHistory();

        toolbar.Controls.Add(_combo);
        toolbar.Controls.Add(send);
        toolbar.Controls.Add(clear);
        layout.Controls.Add(toolbar, 0, 0);

        // ---------- 第二行：历史列表 ----------
        var middle = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = UiTheme.Surface
        };
        middle.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        middle.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        middle.Controls.Add(UiTheme.Label("最近 10 条（双击可重新填入输入框）", UiTheme.SectionFont), 0, 0);

        _list = new ListBox
        {
            Dock = DockStyle.Fill,
            Font = UiTheme.MonoFont,
            BorderStyle = BorderStyle.FixedSingle,
            IntegralHeight = false,
            Margin = new Padding(0)
        };

        // DoubleClick：双击某条历史，把内容填回输入框，光标放到末尾。
        _list.DoubleClick += (_, _) =>
        {
            // is not string text：如果选中的不是字符串（包括 null），直接返回。
            if (_list.SelectedItem is not string text) return;
            _combo.Text = text;
            _combo.Focus();
            _combo.SelectionStart = _combo.Text.Length;
        };
        middle.Controls.Add(_list, 0, 1);
        layout.Controls.Add(middle, 0, 1);

        // ---------- 第三行：状态 + 文件路径 + 接入提示 ----------
        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            AutoSize = true,
            BackColor = UiTheme.Surface,
            Margin = new Padding(0, 10, 0, 0)
        };
        _status = UiTheme.Label("就绪", color: UiTheme.Muted);
        _fileLabel = UiTheme.Label("", color: UiTheme.Muted);
        _fileLabel.Font = new Font("Consolas", 8.5F);
        footer.Controls.Add(_status, 0, 0);
        footer.Controls.Add(_fileLabel, 0, 1);
        footer.Controls.Add(UiTheme.Hint("接入串口助手：SendCurrent() 成功写串口后 AddToHistory(text)；MainForm_FormClosing 里 SaveHistory()；" +
                                         "加载时把 JSON 读回 _history，再刷新 ComboBox.Items。"), 0, 2);
        layout.Controls.Add(footer, 0, 2);

        content.Controls.Add(layout);

        // 从下拉列表选择一项时，把完整文本放进编辑区。
        _combo.SelectedIndexChanged += (_, _) =>
        {
            if (_combo.SelectedItem is string text) _combo.Text = text;
        };

        LoadHistory();
    }

    /// <summary>
    /// 启动时读取历史。读不到文件不算错误：第一次运行本来就没有文件。
    /// 文件损坏、没有权限等异常只提示在界面上，不让程序崩溃。
    /// </summary>
    private void LoadHistory()
    {
        try
        {
            if (File.Exists(HistoryFile))
            {
                var json = File.ReadAllText(HistoryFile);

                // Deserialize<List<string>> 告诉 JSON 库：把文本还原成字符串列表。
                var saved = JsonSerializer.Deserialize<List<string>>(json, JsonOptions);

                // 过滤空白项，并且最多只取 10 条，防止手工改坏文件后界面异常。
                if (saved is not null) _history.AddRange(saved.Where(s => !string.IsNullOrWhiteSpace(s)).Take(10));
            }
        }
        catch (Exception ex)
        {
            _status.Text = $"读取历史失败：{ex.Message}";
            _status.ForeColor = UiTheme.Alarm;
        }

        // 第一次运行：放 3 条示例，让界面不是空的。
        if (_history.Count == 0)
        {
            _history.AddRange(new[]
            {
                "01 03 00 00 00 0A C5 CD",
                "01 06 00 05 03 78 99 19",
                "AT+VER?"
            });
            _status.Text = "首次运行：已放入 3 条示例，发送后会写入 JSON。";
        }

        RefreshViews();
    }

    /// <summary>模拟一次发送：校验 → 更新历史 → 存盘 → 刷新界面。</summary>
    private void SimulateSend()
    {
        var text = _combo.Text.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            MessageBox.Show(this, "发送内容不能为空。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // 去重：如果这条已经存在，先删掉旧的，再插到最前面，
        // 保证“最近使用的排最上面”，不会出现两条一样的。
        _history.RemoveAll(item => string.Equals(item, text, StringComparison.Ordinal));
        _history.Insert(0, text);

        // 只保留 10 条：从索引 10 开始删除多余的部分。
        if (_history.Count > 10) _history.RemoveRange(10, _history.Count - 10);

        SaveHistory();
        RefreshViews();

        _status.Text = $"已模拟发送：{text}（真实项目会在这里写串口并统计 TX 字节）";
        _status.ForeColor = UiTheme.AccentDark;
        _combo.Focus();
        _combo.SelectionStart = _combo.Text.Length;
    }

    /// <summary>清空历史。破坏性操作先弹窗确认。</summary>
    private void ClearHistory()
    {
        if (_history.Count == 0) return;

        if (MessageBox.Show(this, "确定清空最近 10 条发送历史？", "确认",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        _history.Clear();
        SaveHistory();
        RefreshViews();
        _status.Text = "历史已清空。";
        _status.ForeColor = UiTheme.Muted;
    }

    /// <summary>把 _history 序列化成 JSON 写文件；目录不存在时先创建。</summary>
    private void SaveHistory()
    {
        try
        {
            // GetDirectoryName 返回文件所在目录，! 表示我们确定它不是 null。
            var directory = Path.GetDirectoryName(HistoryFile)!;
            Directory.CreateDirectory(directory);

            // new UTF8Encoding(false) 表示 UTF-8 但不写 BOM，避免某些工具读到乱码。
            File.WriteAllText(HistoryFile, JsonSerializer.Serialize(_history, JsonOptions), new UTF8Encoding(false));
            _fileLabel.Text = $"JSON: {HistoryFile}";
        }
        catch (Exception ex)
        {
            _status.Text = $"保存历史失败：{ex.Message}";
            _status.ForeColor = UiTheme.Alarm;
        }
    }

    /// <summary>把内存中的历史同步到 ComboBox 和 ListBox。</summary>
    private void RefreshViews()
    {
        // BeginUpdate/EndUpdate 成对使用：批量修改期间暂停重绘，避免列表闪烁。
        _combo.BeginUpdate();
        var current = _combo.Text;
        _combo.Items.Clear();
        foreach (var item in _history) _combo.Items.Add(item);
        _combo.Text = current;   // 保留用户正在编辑的文字
        _combo.EndUpdate();

        _list.BeginUpdate();
        _list.Items.Clear();
        foreach (var item in _history) _list.Items.Add(item);
        _list.EndUpdate();

        _fileLabel.Text = $"JSON: {HistoryFile}";
    }
}
