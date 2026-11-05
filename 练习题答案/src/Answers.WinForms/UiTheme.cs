namespace Answers.WinForms;

/// <summary>
/// 练习窗体共用的颜色和字体。
///
/// 和旧版不同：这里只放“值”，不再用代码创建控件或布局。
/// 每个窗体的布局、控件位置、表格列都在各自的 Form.Designer.cs 里，
/// 所以 Visual Studio / Rider 的设计器能直接显示窗口样式。
/// </summary>
internal static class UiTheme
{
    public static readonly Color Background = Color.FromArgb(244, 246, 250);
    public static readonly Color Surface = Color.White;
    public static readonly Color SurfaceAlt = Color.FromArgb(248, 250, 252);
    public static readonly Color Text = Color.FromArgb(31, 41, 55);
    public static readonly Color Muted = Color.FromArgb(100, 116, 139);
    public static readonly Color Accent = Color.FromArgb(0, 120, 212);
    public static readonly Color AccentDark = Color.FromArgb(0, 95, 170);
    public static readonly Color AccentSoft = Color.FromArgb(230, 242, 252);
    public static readonly Color Border = Color.FromArgb(226, 232, 240);
    public static readonly Color Alarm = Color.FromArgb(190, 30, 30);
    public static readonly Color AlarmBg = Color.FromArgb(255, 236, 236);
    public static readonly Color Success = Color.FromArgb(21, 128, 61);
    public static readonly Color GridAlt = Color.FromArgb(248, 250, 252);

    public static readonly Font BodyFont = new("Microsoft YaHei UI", 9.5F);
    public static readonly Font SectionFont = new("Microsoft YaHei UI", 11F, FontStyle.Bold);
    public static readonly Font TitleFont = new("Microsoft YaHei UI", 18F, FontStyle.Bold);
    public static readonly Font ResultFont = new("Microsoft YaHei UI", 22F, FontStyle.Bold);
    public static readonly Font MonoFont = new("Consolas", 10F);

    /// <summary>把表格调成统一风格。设计器里已经设置了主要属性，这里再补齐细节。</summary>
    public static void StyleGrid(DataGridView grid)
    {
        grid.BackgroundColor = Surface;
        grid.BorderStyle = BorderStyle.None;
        grid.GridColor = Border;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.EnableHeadersVisualStyles = false;
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
