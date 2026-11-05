namespace Exercises.WinForms.Controls;

/// <summary>
/// 打开双缓冲的 DataGridView。
///
/// 什么是双缓冲：
/// 控件先把整帧画在内存里的“后台画布”上，画完再一次性贴到屏幕。
/// 这样滚动时不会出现“画一半”的闪烁，10 万行虚拟模式滚动会更顺。
///
/// 为什么要单独继承一个类：
/// DataGridView 的 DoubleBuffered 属性是 protected 的，外部不能直接设置。
/// 继承后在本类的构造函数里设置，就能让所有用到它的表格都享受双缓冲。
/// </summary>
internal sealed class BufferedDataGridView : DataGridView
{
    public BufferedDataGridView()
    {
        DoubleBuffered = true;
    }
}
