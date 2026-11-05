namespace Exercises.WinForms;

/// <summary>
/// 练习项目共用的“简易主题”：颜色、字体，以及几个创建控件的辅助方法。
///
/// 这样写的好处：
/// - 所有页面用同一套颜色/字体，不会每个练习各写一遍；
/// - 想改风格只改这一个文件；
/// - 初学者可以把注意力放在界面逻辑上，而不是重复的设置代码。
///
/// 注意：这些控件全部在代码里 new 出来，没有 .Designer.cs 文件，
/// 所以设计器里看到的是空白窗体，运行后才能看到真实界面。
/// </summary>
internal static class UiTheme
{
    // Color.FromArgb(红, 绿, 蓝) 创建颜色，三个数都是 0~255。
    // 这里是一套偏浅色的后台管理界面配色。
    public static readonly Color Background = Color.FromArgb(244, 246, 250);
    public static readonly Color Surface = Color.White;
    public static readonly Color Text = Color.FromArgb(31, 41, 55);
    public static readonly Color Muted = Color.FromArgb(100, 116, 139);
    public static readonly Color Accent = Color.FromArgb(0, 120, 212);
    public static readonly Color AccentDark = Color.FromArgb(0, 95, 170);
    public static readonly Color Border = Color.FromArgb(226, 232, 240);
    public static readonly Color Alarm = Color.FromArgb(190, 30, 30);
    public static readonly Color AlarmBg = Color.FromArgb(255, 236, 236);
    public static readonly Color GridAlt = Color.FromArgb(248, 250, 252);

    // Font(字体名, 字号, 样式)。字号单位是磅（pt）。
    // MonoFont 用等宽字体 Consolas，适合显示十六进制报文，数字对得齐。
    public static readonly Font BodyFont = new("Microsoft YaHei UI", 9.5F);
    public static readonly Font SectionFont = new("Microsoft YaHei UI", 11F, FontStyle.Bold);
    public static readonly Font TitleFont = new("Microsoft YaHei UI", 18F, FontStyle.Bold);
    public static readonly Font ResultFont = new("Microsoft YaHei UI", 22F, FontStyle.Bold);
    public static readonly Font MonoFont = new("Consolas", 10F);

    /// <summary>创建一个自动适应文字宽度的标签。</summary>
    public static Label Label(string text, Font? font = null, Color? color = null) => new()
    {
        Text = text,
        AutoSize = true,
        // ?? 是空合并：调用方没传字体/颜色时，用默认值。
        Font = font ?? BodyFont,
        ForeColor = color ?? Text,
        Margin = new Padding(0, 0, 0, 6)
    };

    /// <summary>创建说明文字：灰色、可自动换行（MaximumSize 限制最大宽度）。</summary>
    public static Label Hint(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Font = BodyFont,
        ForeColor = Muted,
        // MaximumSize 的宽度设上限、高度设 0，Label 就会在超宽时自动换行。
        MaximumSize = new Size(1080, 0),
        Margin = new Padding(0, 6, 0, 0)
    };

    /// <summary>创建一个扁平风格按钮；primary = true 时是蓝底白字的主按钮。</summary>
    public static Button Button(string text, bool primary = false)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            Height = 34,
            MinimumSize = new Size(88, 34),
            Padding = new Padding(12, 0, 12, 0),
            Margin = new Padding(0, 0, 10, 0),
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Accent : Surface,
            ForeColor = primary ? Color.White : Text,
            Cursor = Cursors.Hand,
            Font = BodyFont
        };
        // FlatAppearance 控制扁平按钮的边框和鼠标悬停颜色。
        button.FlatAppearance.BorderSize = primary ? 0 : 1;
        button.FlatAppearance.BorderColor = Border;
        button.FlatAppearance.MouseOverBackColor = primary ? AccentDark : Color.FromArgb(241, 245, 249);
        return button;
    }

    /// <summary>
    /// 创建每个练习页统一的“标题 + 说明 + 内容区”布局。
    /// out 参数 content 把中间的空白面板交给调用方，调用方往里面塞自己的控件。
    ///
    /// TableLayoutPanel 是按“行/列”摆放控件的布局容器，比手算坐标更适应窗口缩放。
    /// </summary>
    public static TableLayoutPanel CreatePage(string title, string description, out Panel content)
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,       // 填满父容器
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(24, 18, 24, 18),
            BackColor = Background
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // 三行：标题自适应高度、说明自适应高度、内容占满剩余空间。
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var titleLabel = Label(title, TitleFont);
        titleLabel.Margin = new Padding(0, 0, 0, 2);

        var descLabel = Hint(description);
        descLabel.Margin = new Padding(0, 0, 0, 8);

        content = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Surface,
            Padding = new Padding(18),
            Margin = new Padding(0)
        };

        // 参数含义：控件, 列, 行。这里都在第 0 列。
        root.Controls.Add(titleLabel, 0, 0);
        root.Controls.Add(descLabel, 0, 1);
        root.Controls.Add(content, 0, 2);
        return root;
    }

    /// <summary>把 DataGridView 调成和主题一致的样式（去边框、隔行变色、整行选中）。</summary>
    public static void StyleGrid(DataGridView grid)
    {
        grid.BackgroundColor = Surface;
        grid.BorderStyle = BorderStyle.None;
        grid.GridColor = Border;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.EnableHeadersVisualStyles = false;   // 关掉系统默认表头，才能自定义颜色
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Text;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font(BodyFont, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(241, 245, 249);
        grid.ColumnHeadersHeight = 34;
        grid.RowTemplate.Height = 28;
        grid.DefaultCellStyle.BackColor = Surface;
        grid.DefaultCellStyle.ForeColor = Text;
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
        grid.DefaultCellStyle.SelectionForeColor = Text;
        grid.DefaultCellStyle.Font = BodyFont;
        grid.AlternatingRowsDefaultCellStyle.BackColor = GridAlt;
        grid.RowHeadersVisible = false;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.MultiSelect = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
    }
}
