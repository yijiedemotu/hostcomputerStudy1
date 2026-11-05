using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using Industrial.Core.Diagnostics;
using Industrial.Core.Models;
using Industrial.Core.Modbus;
using Industrial.Core.Services;
using Industrial.Core.Storage;

namespace ModbusMonitor;

/// <summary>
/// Modbus 采集监控上位机（项目 02）。
///
/// 结构：配置(config/appsettings.json) → 采集引擎(PollingService) → 界面(DataGridView + 趋势图)
///       → 历史库(CSV，按天滚动) → 报警记录。
///
/// 想把它改成真项目，只需三件事：
///   1. 把 appsettings.json 里的 IP/站号/点位地址改成现场实际值；
///   2. 需要数据库就把 CsvHistoryStore 换成 SQLite/SQL Server 实现（见文档第 10 章）；
///   3. 需要权限/配方/报表，按文档第 12 章的架构继续加模块。
/// </summary>
public partial class MainForm : Form
{
    // BindingList 会在增删/属性变化时通知 DataGridView，界面自动刷新
    private readonly BindingList<TagRow> _tags = new();
    private readonly BindingList<AlarmRecord> _alarms = new();

    // 用 "设备名|位号" 作为键，快速找到表格行
    private readonly Dictionary<string, TagRow> _tagIndex = new();

    // 记录每个点上一次的报警级别，用来判断“报警状态是否变化”
    private readonly Dictionary<string, AlarmLevel> _alarmState = new();

    // 采集线程把历史记录塞进队列，UI 定时器批量取出写盘。
    // 这样做的好处：采集线程绝不被磁盘 IO 拖慢，也不会丢数据。
    private readonly ConcurrentQueue<HistoryRecord> _history = new();

    private AppConfig _config = new();          // 当前配置
    private PollingService? _service;           // 采集引擎，null 表示未启动
    private CsvHistoryStore? _store;            // 历史数据存储
    private string _configPath = "";            // 配置文件完整路径
    private string _selectedKey = "";           // 当前选中位号的键
    private long _recordCount;                  // 已写盘的历史条数
    private volatile bool _closing;             // 窗口正在关闭

    public MainForm()
    {
        InitializeComponent();

        // DataGridView 默认不开双缓冲，500ms 刷新一次就会闪。反射打开它是最省事的办法。
        // 因为 DoubleBuffered 是 protected 属性，正常代码访问不到，只能用反射。
        foreach (var grid in new[] { gridTags, gridAlarms })
        {
            typeof(DataGridView)
                .GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)?
                .SetValue(grid, true);
        }

        // 绑定数据源：之后只改 _tags 里的对象，表格自动更新
        gridTags.DataSource = _tags;
        gridAlarms.DataSource = _alarms;
    }

    /// <summary>窗口加载：初始化日志 → 读配置 → 启动“批量写盘”定时器。</summary>
    private void MainForm_Load(object? sender, EventArgs e)
    {
        Log.Init(Path.Combine(AppContext.BaseDirectory, "logs"));
        Log.Info("==== 上位机启动 ====");

        // 订阅日志事件，把日志同步显示到界面的日志列表
        Log.MessageWritten += OnLogMessage;

        LoadConfig();
        tmrFlush.Start();
    }

    // ---------------- 配置与点位 ----------------

    /// <summary>
    /// 读配置并构建界面上的表格行。
    /// 配置坏了不让程序起不来：记日志、提示用户、退回内置默认配置。
    /// </summary>
    private void LoadConfig()
    {
        _configPath = Path.Combine(AppContext.BaseDirectory, "config", "appsettings.json");
        try
        {
            _config = JsonConfigStore.Load(_configPath, CreateDefaultConfig);
        }
        catch (Exception ex)
        {
            Log.Error("配置读取失败，改用内置默认配置", ex);
            MessageBox.Show(
                $"配置文件读取失败，已使用内置默认配置。\n\n{ex.Message}\n\n路径：{_configPath}",
                "配置错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _config = CreateDefaultConfig();
        }

        // 历史数据目录相对于 exe 所在目录
        _store = new CsvHistoryStore(Path.Combine(AppContext.BaseDirectory, _config.DataDirectory));
        Text = $"{_config.Title} · Modbus 监控 Demo";

        BuildRows();
        Log.Info($"配置加载完成：设备 {_config.Devices.Count} 台，点位 {_tags.Count} 个；{_configPath}");
    }

    /// <summary>根据配置重建表格行。重新加载配置时先清空旧行。</summary>
    private void BuildRows()
    {
        _tags.Clear();
        _tagIndex.Clear();
        _alarmState.Clear();
        _selectedKey = "";

        foreach (var device in _config.Devices.Where(d => d.Enabled))
        {
            foreach (var point in device.Points.Where(p => p.Enabled))
            {
                var row = new TagRow(device.Name, point.Name, point.Unit, point.PlcAddress ?? point.Address.ToString());
                _tags.Add(row);
                _tagIndex[device.Name + "|" + point.Name] = row;
            }
        }

        var onlineDevices = _config.Devices.Count(d => d.Enabled);
        lblDeviceState.Text = $"设备：{onlineDevices} 台（未采集）";

        if (_tags.Count > 0)
            gridTags.Rows[0].Selected = true; // 默认选中第一行，趋势图有数据可显示

        trendChart.Reset("未选择位号", "", null, null);
    }

    /// <summary>
    /// 内置默认配置：第一次运行没有 appsettings.json 时自动生成。
    /// 默认连本机 502 端口，配合 tools/modbus_slave_sim.py 模拟器即可看到数据。
    /// </summary>
    private static AppConfig CreateDefaultConfig() => new()
    {
        Title = "1# 锅炉监控上位机",
        DataDirectory = "data",
        Devices =
        {
            new DeviceConfig
            {
                Name = "1#锅炉PLC",
                Protocol = DeviceProtocol.ModbusTcp,
                Host = "127.0.0.1",
                Port = 502,
                SlaveId = 1,
                PollIntervalMs = 500,
                Points =
                {
                    new PointConfig { Name = "炉膛温度", Address = 0, PlcAddress = "40001", DataType = RegisterDataType.Int16, Scale = 0.1, Unit = "℃", LowLimit = 20, HighLimit = 950 },
                    new PointConfig { Name = "蒸汽压力", Address = 1, PlcAddress = "40002", DataType = RegisterDataType.UInt16, Scale = 0.01, Unit = "MPa", HighLimit = 1.2 },
                    new PointConfig { Name = "给水流量", Address = 2, PlcAddress = "40003", DataType = RegisterDataType.UInt16, Scale = 0.1, Unit = "t/h" },
                    new PointConfig { Name = "温度设定值", Address = 3, PlcAddress = "40004", DataType = RegisterDataType.UInt16, Scale = 0.1, Unit = "℃" },
                    new PointConfig { Name = "运行状态", Address = 4, PlcAddress = "40005", DataType = RegisterDataType.UInt16, Unit = "" }
                }
            },
            new DeviceConfig
            {
                Name = "2#液位计（示例，默认关闭）",
                Enabled = false,
                Protocol = DeviceProtocol.ModbusRtu,
                PortName = "COM3",
                BaudRate = 9600,
                SlaveId = 2,
                PollIntervalMs = 1000,
                Points =
                {
                    new PointConfig { Name = "液位", Address = 0, PlcAddress = "40001", DataType = RegisterDataType.Float32, Unit = "m", HighLimit = 8.5 }
                }
            }
        }
    };

    // ---------------- 采集控制 ----------------

    private void btnStart_Click(object? sender, EventArgs e) => StartCollection();

    /// <summary>
    /// 启动采集：创建采集引擎、订阅事件、启动后台循环，并把按钮状态切换成“运行中”。
    /// </summary>
    private void StartCollection()
    {
        if (_service is not null) return; // 防重复启动

        if (_config.Devices.All(d => !d.Enabled))
        {
            MessageBox.Show("配置里没有启用的设备。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _service = new PollingService(_config);
        _service.SnapshotReady += OnSnapshotReady;       // 收到一批数据
        _service.DeviceStateChanged += OnDeviceStateChanged; // 设备连上/掉线
        _service.Start();

        // 运行中不允许重复启动或重载配置，避免状态混乱
        btnStart.Enabled = false;
        btnStop.Enabled = true;
        btnReload.Enabled = false;
        lblMode.Text = "● 采集运行中";
        lblMode.ForeColor = Color.LightGreen;
    }

    // async void 只适合事件处理器；内部异常要自己 try/catch，否则会直接崩进程
    private async void btnStop_Click(object? sender, EventArgs e) => await StopCollectionAsync();

    /// <summary>
    /// 停止采集：先取消订阅，再等引擎真正停下来，最后恢复按钮状态。
    /// await 让 UI 线程在等待期间仍能响应，不会假死。
    /// </summary>
    private async Task StopCollectionAsync()
    {
        var service = _service;
        if (service is null) return;

        _service = null;

        // 先退订，避免停止过程中还有事件进来更新界面
        service.SnapshotReady -= OnSnapshotReady;
        service.DeviceStateChanged -= OnDeviceStateChanged;
        btnStop.Enabled = false;

        try
        {
            await service.StopAsync();
        }
        catch (Exception ex)
        {
            Log.Warn($"停止采集时异常：{ex.Message}");
        }

        btnStart.Enabled = true;
        btnReload.Enabled = true;
        lblMode.Text = "已停止";
        lblMode.ForeColor = Color.Gray;
        lblDeviceState.Text = "设备：未采集";
    }

    /// <summary>重新加载配置：先停采集，再读文件、重建表格。</summary>
    private async void btnReload_Click(object? sender, EventArgs e)
    {
        await StopCollectionAsync();
        LoadConfig();
        Log.Info("配置已重新加载。");
        lblTips.Text = "配置已重新加载，点击“启动采集”生效";
    }

    /// <summary>用资源管理器打开历史数据目录。</summary>
    private void btnOpenFolder_Click(object? sender, EventArgs e)
    {
        if (_store is null) return;
        try
        {
            // UseShellExecute = true 才会交给系统用默认程序打开
            Process.Start(new ProcessStartInfo("explorer.exe", _store.DirectoryPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Log.Warn($"打开数据目录失败：{ex.Message}");
        }
    }

    // ---------------- 采集事件 ----------------

    /// <summary>
    /// 采集引擎在后台线程触发这个事件。
    /// 绝对不能在这里直接改控件，必须 BeginInvoke 切回 UI 线程。
    /// </summary>
    private void OnSnapshotReady(IReadOnlyList<TagValue> values)
    {
        if (_closing || IsDisposed) return;
        try
        {
            // BeginInvoke：把 ApplySnapshot 排进 UI 消息队列，立即返回
            BeginInvoke(new Action<IReadOnlyList<TagValue>>(ApplySnapshot), values);
        }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) { /* 窗口已关闭，BeginInvoke 会抛这个 */ }
    }

    /// <summary>
    /// 在 UI 线程应用一批采集结果：
    /// 更新表格 → 入历史队列 → 更新趋势图 → 判断报警状态变化。
    /// </summary>
    private void ApplySnapshot(IReadOnlyList<TagValue> values)
    {
        foreach (var v in values)
        {
            var key = v.DeviceName + "|" + v.TagName;
            if (!_tagIndex.TryGetValue(key, out var row)) continue;

            var alarmText = v.Alarm switch
            {
                AlarmLevel.High => "超上限",
                AlarmLevel.Low => "低于下限",
                _ => ""
            };

            // 更新表格行；PropertyChanged 会让 DataGridView 自动重绘
            row.Update(v.Value.ToString("0.###"), v.Timestamp, v.QualityText, alarmText,
                isBad: v.Quality == Quality.Bad, isAlarm: v.IsAlarm);

            if (v.Quality == Quality.Good)
            {
                // 好数据才入库；坏数据入库会污染历史曲线
                _history.Enqueue(new HistoryRecord(v.Timestamp, v.DeviceName, v.TagName, v.Value, v.Unit, v.Quality, v.Alarm));

                // 只有当前选中的位号才加到趋势图
                if (_selectedKey == key)
                    trendChart.AddPoint(v.Timestamp, v.Value);
            }

            // 报警只在“状态发生变化”时记一条，避免刷屏：
            // 超限持续 10 分钟也只记一次“触发”，恢复时再记一次“恢复”。
            var previous = _alarmState.TryGetValue(key, out var level) ? level : AlarmLevel.None;
            if (previous == v.Alarm) continue;

            _alarmState[key] = v.Alarm;
            if (v.Alarm != AlarmLevel.None)
            {
                _alarms.Insert(0, new AlarmRecord(v.Timestamp, v.DeviceName, v.TagName, v.Value, alarmText, "触发"));
                Log.Warn($"报警触发：{v.DeviceName}/{v.TagName} = {v.Value:0.###}{v.Unit}（{alarmText}）");
            }
            else if (previous != AlarmLevel.None)
            {
                var previousText = previous == AlarmLevel.High ? "超上限" : "低于下限";
                _alarms.Insert(0, new AlarmRecord(v.Timestamp, v.DeviceName, v.TagName, v.Value, previousText, "恢复"));
                Log.Info($"报警恢复：{v.DeviceName}/{v.TagName} = {v.Value:0.###}{v.Unit}");
            }

            // 报警表最多保留 500 条，防止长时间运行内存膨胀
            while (_alarms.Count > 500) _alarms.RemoveAt(_alarms.Count - 1);
        }
    }

    private void OnDeviceStateChanged(string message, bool online)
    {
        if (_closing || IsDisposed) return;
        try
        {
            BeginInvoke(new Action(() =>
            {
                lblDeviceState.Text = online ? $"设备：{message}" : $"设备：{message}";
                lblDeviceState.ForeColor = online ? Color.LightGreen : Color.OrangeRed;
            }));
        }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) { }
    }

    // ---------------- 表格交互 ----------------

    /// <summary>切换选中位号时，清空趋势图并套用该点位的单位和报警限。</summary>
    private void gridTags_SelectionChanged(object? sender, EventArgs e)
    {
        if (gridTags.CurrentRow?.DataBoundItem is not TagRow row) return;

        var key = row.Device + "|" + row.Tag;
        if (key == _selectedKey) return; // 同一行反复触发，不用重建
        _selectedKey = key;

        var point = FindPoint(row.Device, row.Tag);
        trendChart.Reset($"{row.Device} · {row.Tag}", row.Unit, point?.LowLimit, point?.HighLimit);
    }

    /// <summary>
    /// 单元格着色：坏数据灰掉，报警行红底。
    /// CellFormatting 会在每个单元格显示前调用，所以这里只做轻量判断。
    /// </summary>
    private void gridTags_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0) return;
        if (gridTags.Rows[e.RowIndex].DataBoundItem is not TagRow row) return;

        e.CellStyle ??= new DataGridViewCellStyle();

        if (row.IsBad)
        {
            e.CellStyle.BackColor = Color.FromArgb(38, 38, 42);
            e.CellStyle.ForeColor = Color.Gray;
        }
        else if (row.IsAlarm)
        {
            e.CellStyle.BackColor = Color.FromArgb(74, 32, 32);
            e.CellStyle.ForeColor = Color.OrangeRed;
        }
    }

    /// <summary>右键“复制数值”到剪贴板。</summary>
    private void miCopy_Click(object? sender, EventArgs e)
    {
        if (gridTags.CurrentRow?.DataBoundItem is not TagRow row) return;
        if (row.Value == "--") return;
        try { Clipboard.SetText(row.Value); } catch { }
    }

    /// <summary>写单个点位：工程值 → 原始值 → 寄存器 → 06/10 功能码。</summary>
    private async void miWrite_Click(object? sender, EventArgs e)
    {
        if (gridTags.CurrentRow?.DataBoundItem is not TagRow row) return;

        var device = _config.Devices.FirstOrDefault(d => d.Name == row.Device);
        var point = device?.Points.FirstOrDefault(p => p.Name == row.Tag);
        if (device is null || point is null)
        {
            MessageBox.Show("找不到该点位的配置，无法写入。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var input = InputDialog.Show(this, "写寄存器",
            $"{device.Name} / {point.Name}（{point.PlcAddress ?? point.Address.ToString()}）\n请输入工程值（{point.Unit}）：",
            row.Value == "--" ? "0" : row.Value);
        if (input is null) return; // 用户点了取消

        if (!double.TryParse(input, out var engineering))
        {
            MessageBox.Show("请输入数字。", "输入有误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // 工程值 → 原始值（除 Scale、减 Offset）→ 寄存器数组（按类型和字序编码）
        var raw = RegisterCodec.ToRaw(engineering, point.Scale, point.Offset);
        var registers = RegisterCodec.Encode(raw, point.DataType, point.Order);

        try
        {
            // 写入用独立连接，写完就释放（using），不和采集引擎抢同一条连接
            using var master = ModbusMasterFactory.Create(device);
            await master.ConnectAsync();

            if (registers.Length == 1)
                await master.WriteSingleRegisterAsync(device.SlaveId, point.Address, registers[0]); // 06
            else
                await master.WriteMultipleRegistersAsync(device.SlaveId, point.Address, registers); // 10

            Log.Info($"写入成功：{device.Name}/{point.Name} = {engineering:0.###}{point.Unit}（原始值 {raw:0.###}）");
            lblTips.Text = $"写入成功：{point.Name} = {engineering:0.###}{point.Unit}";
        }
        catch (Exception ex)
        {
            Log.Error($"写入失败：{ex.Message}", ex);
            MessageBox.Show($"写入失败：\n\n{ex.Message}", "写入失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private PointConfig? FindPoint(string deviceName, string tagName) =>
        _config.Devices.FirstOrDefault(d => d.Name == deviceName)?.Points.FirstOrDefault(p => p.Name == tagName);

    // ---------------- 历史与日志 ----------------

    /// <summary>定时器（默认 1 秒）：把队列里的历史数据批量写盘，并刷新设备状态文字。</summary>
    private void tmrFlush_Tick(object? sender, EventArgs e)
    {
        FlushHistory();

        // 没有处于故障状态时，显示“N 台采集中”
        if (_service?.IsRunning == true && lblDeviceState.ForeColor != Color.OrangeRed)
        {
            var online = _config.Devices.Count(d => d.Enabled);
            lblDeviceState.Text = $"设备：{online} 台采集中";
        }
    }

    /// <summary>
    /// 批量落盘：一次最多取 5000 条，减少文件打开/关闭次数。
    /// 即使写盘失败也只记日志，不能让采集停掉。
    /// </summary>
    private void FlushHistory()
    {
        if (_store is null) return;

        var batch = new List<HistoryRecord>();
        while (batch.Count < 5000 && _history.TryDequeue(out var record))
            batch.Add(record);

        if (batch.Count == 0) return;

        try
        {
            _store.Append(batch);
            _recordCount += batch.Count;
            lblRecords.Text = $"历史 {_recordCount} 条";
        }
        catch (Exception ex)
        {
            Log.Error($"历史数据写入失败（磁盘满/文件被占用？）：{ex.Message}", ex);
        }
    }

    /// <summary>日志事件可能在任意线程触发，同样要 BeginInvoke 回 UI 线程再改控件。</summary>
    private void OnLogMessage(string line)
    {
        if (_closing || IsDisposed) return;
        try
        {
            BeginInvoke(new Action(() =>
            {
                // 日志列表最多保留 1000 条
                if (listLog.Items.Count > 1000) listLog.Items.RemoveAt(0);
                listLog.Items.Add(line);
                listLog.TopIndex = Math.Max(0, listLog.Items.Count - 1); // 自动滚到底
            }));
        }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) { }
    }

    /// <summary>
    /// 窗口关闭：停止采集、把队列里剩余历史全部落盘、写退出日志。
    /// 注意 .Wait(1500) 会短暂阻塞 UI，但这是关闭流程，可以接受；
    /// 加超时是防止串口卡死导致窗口永远关不掉。
    /// </summary>
    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        _closing = true;
        tmrFlush.Stop();
        Log.MessageWritten -= OnLogMessage;

        try { _service?.StopAsync().Wait(1500); } catch { }
        FlushHistory();
        Log.Info("==== 上位机退出 ====");
    }
}
