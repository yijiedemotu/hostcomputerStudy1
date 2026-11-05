namespace Answers.WinForms.Models;

/// <summary>
/// 报警表格的一行数据。
///
/// 属性名（Time / Device / Tag / Value / Status / Message）
/// 会和 DataGridView 列的 DataPropertyName 一一对应，
/// 所以表格只要设置 DataSource，就能自动把值显示到对应列。
/// </summary>
public sealed record AlarmRow(
    DateTime Time,
    string Device,
    string Tag,
    double Value,
    string Status,
    string Message);
