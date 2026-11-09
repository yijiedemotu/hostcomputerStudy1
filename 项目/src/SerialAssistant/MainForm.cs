using System.Text;
using System.Text.RegularExpressions;
using Industrial.Core.Diagnostics;
using Industrial.Core.Transport;

namespace SerialAssistant;

/// <summary>
/// 串口调试助手：上位机工程师的“万用表”。
///
/// 功能：串口参数配置 / HEX 与文本收发 / 时间戳 / 定时发送 / 收发计数 / 日志保存。
/// 技术点：
///   * 串口读放在后台 Task，UI 用 BeginInvoke 更新——不会卡界面；
///   * HEX 与文本两种视图，文本编码支持 UTF-8/GB2312（国产仪表多用 GBK）；
///   * 打开失败给出中文原因（占用/不存在/被拔出），这是现场排障的第一课。
/// </summary>
public partial class MainForm : Form
{
    // 当前打开的串口；null 表示未打开。
    // 用接口而不是具体类：物理口走 SerialPortNative(Win32)，
    // 免驱动虚拟口 VCOM1/VCOM2 走 VirtualSerialPort(TCP 回环)，业务代码完全不用改。
    private ISerialPort? _port;

    // 文本模式的编码，默认 UTF-8；国产仪表常需要切到 GB2312
    private Encoding _encoding = new UTF8Encoding(false);

    // 后台接收任务
    private Task? _receiveTask;

    // 收/发字节计数。用 long + Interlocked，因为接收线程和 UI 线程都会改
    private long _rxBytes;
    private long _txBytes;

    // 窗口正在关闭的标志。接收线程靠它尽快退出，用 volatile 保证线程间可见
    private volatile bool _closing;

    public MainForm()
    {
        InitializeComponent(); // 由 Designer 生成，创建所有控件
        InitCombos();          // 填充下拉框并绑定事件
    }

    /// <summary>初始化串口参数下拉框。界面设计器里没放选项，统一在这里填。</summary>
    private void InitCombos()
    {
        cboBaud.Items.AddRange(new object[] { 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200, 230400, 460800, 921600 });
        cboBaud.SelectedItem = 9600; // 工业仪表最常见

        cboDataBits.Items.AddRange(new object[] { 5, 6, 7, 8 });
        cboDataBits.SelectedItem = 8;

        cboParity.Items.AddRange(new object[] { "None", "Odd", "Even", "Mark", "Space" });
        cboParity.SelectedIndex = 0;

        cboStopBits.Items.AddRange(new object[] { "1", "1.5", "2" });
        cboStopBits.SelectedIndex = 0;

        cboEncoding.Items.AddRange(new object[] { "UTF-8", "GB2312", "ASCII" });
        cboEncoding.SelectedIndex = 0;

        // 切换编码时，换掉 _encoding 字段；下次收到数据就按新编码解码
        cboEncoding.SelectedIndexChanged += (_, _) => _encoding = cboEncoding.SelectedIndex switch
        {
            1 => Encoding.GetEncoding("GB2312"),
            2 => Encoding.ASCII,
            _ => new UTF8Encoding(false)
        };

        // 修改定时间隔时，立即同步到定时器
        numInterval.ValueChanged += (_, _) => tmrAutoSend.Interval = (int)numInterval.Value;
    }

    /// <summary>窗体加载完成：枚举串口，并把界面设成“未打开”状态。</summary>
    private void MainForm_Load(object? sender, EventArgs e)
    {
        RefreshPortList();
        SetUiOpen(false);
    }

    /// <summary>
    /// 刷新串口列表。插拔 USB 转串口后点“刷新”就会重新读注册表。
    /// 列表末尾固定追加 VCOM1 / VCOM2 两个免驱动虚拟口（由 VComPair 工具提供），
    /// 这样一台没有串口、也没装 com0com 的电脑也能完整练习收发。
    /// </summary>
    private void RefreshPortList()
    {
        var current = cboPort.Text.Trim();
        var physical = SerialPortNative.GetPortNames();

        var ports = new string[physical.Length + VirtualSerialPort.VirtualPortNames.Length];
        Array.Copy(physical, ports, physical.Length);
        Array.Copy(VirtualSerialPort.VirtualPortNames, 0, ports,
            physical.Length, VirtualSerialPort.VirtualPortNames.Length);

        cboPort.Items.Clear();
        cboPort.Items.AddRange(ports);

        // 保留当前选择/手输的端口；初始为空时默认给一个免驱动虚拟口
        cboPort.Text = string.IsNullOrWhiteSpace(current)
            ? VirtualSerialPort.VirtualPortNames[0]
            : current;

        // 没检测到物理串口时给出提示，并明确告诉用户还有一条免驱动的路
        lblTips.Text = physical.Length == 0
            ? "未检测到物理串口：可选 VCOM1/VCOM2（先启动『虚拟串口对工具 VComPair』），或检查 CH340/CP2102/FT232 驱动"
            : $"检测到 {physical.Length} 个物理串口：{string.Join("、", physical)}；另可用 VCOM1/VCOM2 虚拟串口";
    }

    private void btnRefreshPorts_Click(object? sender, EventArgs e) => RefreshPortList();

    /// <summary>“打开/关闭串口”按钮：已打开就关闭，未打开就按界面参数打开。</summary>
    private void btnOpen_Click(object? sender, EventArgs e)
    {
        if (_port is not null) { ClosePort(); return; }

        // 把界面上的选项收集成串口参数对象
        var settings = new SerialSettings
        {
            PortName = cboPort.Text.Trim(),
            BaudRate = (int)(cboBaud.SelectedItem ?? 9600),
            DataBits = (int)(cboDataBits.SelectedItem ?? 8),
            Parity = (ParityMode)cboParity.SelectedIndex,
            StopBits = cboStopBits.SelectedIndex switch
            {
                1 => StopBitsMode.OnePointFive,
                2 => StopBitsMode.Two,
                _ => StopBitsMode.One
            },
            ReadTimeoutMs = 50 // 接收循环每次最多阻塞 50ms，方便及时响应关闭
        };

        if (string.IsNullOrWhiteSpace(settings.PortName))
        {
            MessageBox.Show("请先选择或输入串口号，例如 COM3。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // 同一个 ISerialPort 接口，两种实现：
        //   COMx   → SerialPortNative，走 Win32 CreateFile 打开真实/驱动虚拟串口
        //   VCOMx  → VirtualSerialPort，走本机 TCP 回环连到 VComPair 工具（零驱动、零管理员权限）
        ISerialPort port = VirtualSerialPort.IsVirtualPort(settings.PortName)
            ? new VirtualSerialPort(settings.PortName.Trim().ToUpperInvariant())
            : new SerialPortNative(settings);
        try
        {
            port.Open();
        }
        catch (Exception ex)
        {
            port.Dispose(); // 打开失败也要释放句柄/连接
            ShowError("打开串口失败", ex);
            return;
        }

        _port = port;
        Interlocked.Exchange(ref _rxBytes, 0);
        Interlocked.Exchange(ref _txBytes, 0);
        SetUiOpen(true);
        StartReceiveLoop(); // 启动后台收数
        Log.Info($"打开串口 {settings}，超时 {settings.ReadTimeoutMs}ms");
        lblTips.Text = VirtualSerialPort.IsVirtualPort(settings.PortName)
            ? $"已打开虚拟串口 {settings.PortName}（参数仅作显示，虚拟链路不限制波特率），等待数据…"
            : $"已打开 {settings}，等待数据…";
    }

    /// <summary>
    /// 关闭串口。顺序很重要：先停定时器 → 关端口 → 等接收线程退出 → 再恢复界面。
    /// 如果先恢复界面，用户可能马上又点打开，导致端口还没释放。
    /// </summary>
    private void ClosePort()
    {
        tmrAutoSend.Stop();
        if (chkAutoSend.Checked) chkAutoSend.Checked = false;

        var port = _port;
        _port = null;

        try { port?.Close(); } catch (Exception ex) { Log.Warn($"关闭串口异常：{ex.Message}"); }

        // 等接收线程最多 500ms 退出。Read 有 50ms 超时，正常很快就能退出。
        try { _receiveTask?.Wait(500); } catch { }

        _receiveTask = null;
        SetUiOpen(false);
        lblTips.Text = "串口已关闭";
        Log.Info("串口已关闭");
    }

    /// <summary>根据串口状态切换按钮文字和参数控件的可用性。</summary>
    private void SetUiOpen(bool open)
    {
        btnOpen.Text = open ? "关闭串口" : "打开串口";

        // 串口打开期间不允许改参数，否则参数和实际端口不一致
        foreach (var c in new Control[] { cboPort, cboBaud, cboDataBits, cboParity, cboStopBits, btnRefreshPorts })
            c.Enabled = !open;

        lblPortState.Text = open ? $"已打开 {_port?.PortName}" : "未打开";
        lblPortState.ForeColor = open ? Color.SeaGreen : Color.Firebrick;
    }

    /// <summary>
    /// 后台接收循环。串口 Read 带 50ms 超时，读不到就继续下一轮，
    /// 这样既能及时退出，又不会空转吃满 CPU。
    /// </summary>
    private void StartReceiveLoop()
    {
        var port = _port;
        if (port is null) return;

        // Task.Run 把循环丢到线程池，UI 线程立刻返回，窗口不会卡
        _receiveTask = Task.Run(() =>
        {
            var buffer = new byte[8192];
            while (!_closing && _port is not null)
            {
                try
                {
                    // 阻塞读，最多等 50ms。返回 0 表示这段没数据，继续下一轮。
                    var n = port.Read(buffer, 0, buffer.Length, 50);
                    if (n <= 0) continue;

                    // 只取真正读到的那 n 个字节
                    var chunk = new byte[n];
                    Buffer.BlockCopy(buffer, 0, chunk, 0, n);
                    Interlocked.Add(ref _rxBytes, n);

                    // 窗口已关闭就不能再 BeginInvoke，否则会抛异常
                    if (_closing || IsDisposed) break;

                    // 关键：接收线程不能直接改控件，必须 BeginInvoke 切回 UI 线程
                    // BeginInvoke 是“异步”的，不会阻塞接收循环
                    BeginInvoke(new Action<byte[]>(AppendReceived), chunk);
                }
                catch (ObjectDisposedException) { break; }   // 端口已释放
                catch (InvalidOperationException) { break; } // 端口已关闭
                catch (IOException) { break; }               // 设备被拔出
                catch (Exception ex)
                {
                    Log.Error($"接收线程异常：{ex.Message}", ex);
                    break;
                }
            }
        });
    }

    /// <summary>
    /// 把收到的字节显示到接收框。这个方法一定在 UI 线程执行（由 BeginInvoke 切过来），
    /// 所以可以直接操作控件。
    /// </summary>
    private void AppendReceived(byte[] data)
    {
        if (chkPause.Checked) return; // 勾了“暂停显示”就只计数、不显示

        var sb = new StringBuilder();

        // 可选：每行前面加时间戳，排查“多久来一帧”很有用
        if (chkTimestamp.Checked) sb.Append('[').Append(DateTime.Now.ToString("HH:mm:ss.fff")).Append("] ");

        if (chkHexDisplay.Checked)
        {
            // HEX 模式：每个字节显示成两位十六进制，例如 01 03 00 00
            foreach (var b in data) sb.Append(b.ToString("X2")).Append(' ');
            sb.AppendLine();
        }
        else
        {
            // 文本模式若收到裸 \r，补成 \r\n，否则 RichTextBox 会显示成异常换行。
            // 正则 "\r(?!\n)" 的意思是“后面不是 \n 的 \r”。
            var text = _encoding.GetString(data);
            sb.Append(Regex.Replace(text, "\r(?!\n)", "\r\n"));
        }

        txtReceive.AppendText(sb.ToString());

        // 长时间运行必须限制文本量，否则 RichTextBox 会越来越慢直到卡死。
        // 超过 40 万字符就一次删掉最前面的 20 万字符。
        if (txtReceive.TextLength > 400_000)
        {
            txtReceive.Select(0, 200_000);
            txtReceive.SelectedText = string.Empty;
        }

        // 自动滚到底部
        if (chkAutoScroll.Checked)
        {
            txtReceive.SelectionStart = txtReceive.TextLength;
            txtReceive.ScrollToCaret();
        }
    }

    private void btnSend_Click(object? sender, EventArgs e) => SendCurrent();

    /// <summary>发送当前输入框的内容。按钮、Ctrl+Enter、定时器都调用它。</summary>
    private void SendCurrent()
    {
        if (_port is null)
        {
            MessageBox.Show("请先打开串口。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        byte[] payload;
        try
        {
            payload = BuildPayload();
        }
        catch (FormatException ex)
        {
            // HEX 输入格式错误：提示用户，不发送
            MessageBox.Show(ex.Message, "发送内容格式错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (payload.Length == 0) return;

        try
        {
            _port.Write(payload);
            Interlocked.Add(ref _txBytes, payload.Length);

            // 开了 HEX + 时间戳时，把发送内容也回显到接收区，方便对照
            if (chkHexDisplay.Checked && chkTimestamp.Checked)
                txtReceive.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] TX → {Convert.ToHexString(payload).Chunked()}{Environment.NewLine}");
        }
        catch (Exception ex)
        {
            ShowError("发送失败", ex);
            ClosePort(); // 发送失败通常意味着线被拔了，直接关闭端口
        }
    }

    /// <summary>把发送框内容转换成要发送的字节数组（HEX 或文本，可追加换行）。</summary>
    private byte[] BuildPayload()
    {
        var text = txtSend.Text;
        byte[] data = chkHexSend.Checked ? ParseHex(text) : _encoding.GetBytes(text);

        // 很多仪表按“回车换行”结束一条命令，这里可选追加 \r\n
        if (chkAppendNewLine.Checked)
        {
            var withCrLf = new byte[data.Length + 2];
            data.CopyTo(withCrLf, 0);
            withCrLf[^2] = 0x0D; // \r
            withCrLf[^1] = 0x0A; // \n
            data = withCrLf;
        }
        return data;
    }

    /// <summary>
    /// 解析 "01 03 00 00 00 0A"、"0103000000 0A"、"0x01,0x03" 等写法。
    /// 先过滤掉空格、逗号、0x 等装饰字符，剩下的必须是偶数个十六进制字符。
    /// </summary>
    internal static byte[] ParseHex(string text)
    {
        var cleaned = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];

            // 分隔符直接跳过
            if (c is ' ' or '\r' or '\n' or '\t' or ',' or '-' or ';') continue;

            // 跳过 "0x" 前缀：看见 0 后面跟 x，就把 i 多推一位
            if ((c == '0') && i + 1 < text.Length && (text[i + 1] is 'x' or 'X')) { i++; continue; }

            if (!Uri.IsHexDigit(c))
                throw new FormatException($"HEX 内容包含非法字符 '{c}'（位置 {i + 1}）。只允许 0-9 A-F 与空格。");
            cleaned.Append(c);
        }

        if (cleaned.Length == 0) return Array.Empty<byte>();
        if (cleaned.Length % 2 != 0)
            throw new FormatException($"HEX 字符个数必须是偶数，当前 {cleaned.Length} 个。例如 01 03 应写成 0103。");

        // 每两个字符拼成一个字节：Convert.ToByte(子串, 16) 表示按十六进制解析
        var bytes = new byte[cleaned.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
            bytes[i] = Convert.ToByte(cleaned.ToString(i * 2, 2), 16);
        return bytes;
    }

    /// <summary>勾选/取消“定时发送”。勾上时先检查串口是否已打开。</summary>
    private void chkAutoSend_CheckedChanged(object? sender, EventArgs e)
    {
        if (!chkAutoSend.Checked)
        {
            tmrAutoSend.Stop();
            return;
        }

        if (_port is null)
        {
            chkAutoSend.Checked = false;
            MessageBox.Show("定时发送前请先打开串口。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        tmrAutoSend.Interval = (int)numInterval.Value;
        tmrAutoSend.Start();
        lblTips.Text = $"定时发送已开启，每 {tmrAutoSend.Interval}ms 一帧";
    }

    /// <summary>定时器到点：重复发送输入框里的内容。</summary>
    private void tmrAutoSend_Tick(object? sender, EventArgs e)
    {
        if (_port is null) { tmrAutoSend.Stop(); chkAutoSend.Checked = false; return; }
        SendCurrent();
    }

    /// <summary>状态栏定时刷新收发字节数。</summary>
    private void tmrStatus_Tick(object? sender, EventArgs e)
    {
        lblRx.Text = $"收 {Interlocked.Read(ref _rxBytes)} 字节";
        lblTx.Text = $"发 {Interlocked.Read(ref _txBytes)} 字节";
    }

    /// <summary>清空接收区，同时把计数归零。</summary>
    private void btnClear_Click(object? sender, EventArgs e)
    {
        txtReceive.Clear();
        Interlocked.Exchange(ref _rxBytes, 0);
        Interlocked.Exchange(ref _txBytes, 0);
    }

    private void btnClearSend_Click(object? sender, EventArgs e) => txtSend.Clear();

    /// <summary>把接收区内容保存成文本文件（带 BOM，Windows 记事本打开不乱码）。</summary>
    private void btnSaveLog_Click(object? sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "文本文件|*.txt|日志文件|*.log|所有文件|*.*",
            FileName = $"serial-{DateTime.Now:yyyyMMdd-HHmmss}.txt"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            File.WriteAllText(dialog.FileName, txtReceive.Text, new UTF8Encoding(true));
            lblTips.Text = $"已保存到 {dialog.FileName}";
        }
        catch (Exception ex)
        {
            ShowError("保存日志失败", ex);
        }
    }

    /// <summary>Ctrl+Enter 快捷发送。SuppressKeyPress 阻止回车在输入框里换行。</summary>
    private void MainForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            SendCurrent();
        }
    }

    /// <summary>窗口关闭：先置关闭标志，让接收线程退出，再关串口。</summary>
    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        _closing = true;
        tmrStatus.Stop();
        ClosePort();
    }

    /// <summary>统一的错误处理：写日志 + 弹窗，避免每处都写两遍。</summary>
    private void ShowError(string title, Exception ex)
    {
        Log.Error($"{title}：{ex.Message}", ex);
        MessageBox.Show($"{title}\n\n{ex.Message}", title, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}

/// <summary>把 "01030000" 这样的连续十六进制串每两位加一个空格，便于阅读。</summary>
internal static class HexFormatExtensions
{
    /// <summary>把 "AA BB CC" 形式的字符串按每两个字符切开。</summary>
    public static string Chunked(this string hex)
    {
        var parts = new string[(hex.Length + 1) / 2];
        for (var i = 0; i < parts.Length; i++)
            parts[i] = hex.Substring(i * 2, Math.Min(2, hex.Length - i * 2));
        return string.Join(' ', parts);
    }
}
