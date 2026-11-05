using System.Globalization;
using System.Text;
using Industrial.Core.Models;

namespace Industrial.Core.Storage;

/// <summary>
/// 按天滚动的 CSV 历史库（零依赖版）。
///
/// 为什么先教 CSV？
///  * 上位机的历史数据本质是“时序点表”，CSV 够用、能用 Excel 打开、现场排障最方便；
///  * 真正上规模（百万行以上、要多条件查询）才换 SQLite/SQL Server，
///    SQL 建表语句见《10 数据存储》一章，接口层可以平滑替换。
///
/// 写入策略：同一批采集数据一次写入、每行立即 flush，断电最多丢最后一行。
/// 采集线程和 UI 线程会同时写，所以内部用 lock 串行化。
/// </summary>
public sealed class CsvHistoryStore
{
    private readonly string _directory;

    // 采集线程和 UI 线程可能同时写，用一把锁把写入串行化，避免文件内容交错
    private readonly object _sync = new();

    // InvariantCulture 保证小数点是 "."，不会因为系统区域设置变成逗号而破坏 CSV 格式
    private readonly CultureInfo _culture = CultureInfo.InvariantCulture;

    public CsvHistoryStore(string directory)
    {
        _directory = directory;
        Directory.CreateDirectory(_directory); // 目录不存在就自动创建
    }

    public string DirectoryPath => _directory;

    /// <summary>某一天对应的文件路径：data/20261023.csv。</summary>
    public string GetFilePath(DateTime time) =>
        Path.Combine(_directory, $"{time:yyyyMMdd}.csv");

    /// <summary>追加一批历史记录。整批写完统一 Flush，兼顾速度和断电安全。</summary>
    public void Append(IEnumerable<HistoryRecord> records)
    {
        // as + ?? 的写法：如果本来就是 List 就直接用，否则复制成 List（后面要多次遍历）
        var list = records as IList<HistoryRecord> ?? records.ToList();
        if (list.Count == 0) return;

        lock (_sync)
        {
            // 跨天时同一次写入可能落在两个文件里，按日期分组。
            foreach (var group in list.GroupBy(r => r.Timestamp.Date))
            {
                var path = GetFilePath(group.Key);

                // 文件不存在或为空，说明要写表头
                var isNew = !File.Exists(path) || new FileInfo(path).Length == 0;

                // FileShare.Read：写入时允许 Excel/记事本同时读取
                using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
                using var writer = new StreamWriter(stream, new UTF8Encoding(true)); // 带 BOM，Excel 打开不乱码
                if (isNew)
                    writer.WriteLine("时间,设备,位号,数值,单位,质量,报警");

                foreach (var r in group)
                {
                    // 逐个字段写，含逗号的字段用 Escape 加引号
                    writer.Write(r.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff", _culture));
                    writer.Write(','); writer.Write(Escape(r.Device));
                    writer.Write(','); writer.Write(Escape(r.Tag));
                    writer.Write(','); writer.Write(r.Value.ToString("0.######", _culture));
                    writer.Write(','); writer.Write(Escape(r.Unit));
                    writer.Write(','); writer.Write(r.Quality);
                    writer.Write(','); writer.Write(r.Alarm);
                    writer.WriteLine();
                }
                writer.Flush(); // 刷到磁盘，断电最多丢最后一行
            }
        }
    }

    /// <summary>读取某一天的历史数据（演示用；数据量大时请改用数据库）。</summary>
    public List<HistoryRecord> ReadDay(DateTime date, int maxRows = 200_000)
    {
        var path = GetFilePath(date);
        var result = new List<HistoryRecord>();
        if (!File.Exists(path)) return result;

        lock (_sync)
        {
            using var reader = new StreamReader(path, Encoding.UTF8, true);
            reader.ReadLine(); // 跳过表头

            while (!reader.EndOfStream && result.Count < maxRows)
            {
                var line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var cells = SplitCsv(line);
                if (cells.Count < 7) continue; // 列数不对，跳过这行坏数据

                // TryParse 系列：解析失败返回 false，不会抛异常，适合处理外部文件
                if (!DateTime.TryParseExact(cells[0], "yyyy-MM-dd HH:mm:ss.fff", _culture,
                        DateTimeStyles.None, out var ts)) continue;
                if (!double.TryParse(cells[3], NumberStyles.Float, _culture, out var value)) continue;

                Enum.TryParse<Quality>(cells[5], out var quality);
                Enum.TryParse<AlarmLevel>(cells[6], out var alarm);
                result.Add(new HistoryRecord(ts, cells[1], cells[2], value, cells[4], quality, alarm));
            }
        }
        return result;
    }

    /// <summary>清理 N 天以前的文件。上位机的硬盘通常很小，必须自动清理。</summary>
    public int Cleanup(DateTime now, int keepDays)
    {
        var cutoff = now.Date.AddDays(-keepDays);
        var deleted = 0;

        foreach (var file in Directory.EnumerateFiles(_directory, "*.csv"))
        {
            // 文件名就是日期，例如 20261023.csv
            var name = Path.GetFileNameWithoutExtension(file);
            if (DateTime.TryParseExact(name, "yyyyMMdd", _culture, DateTimeStyles.None, out var date) && date < cutoff)
            {
                try { File.Delete(file); deleted++; } catch { /* 文件被 Excel 占用时跳过 */ }
            }
        }
        return deleted;
    }

    /// <summary>
    /// CSV 转义：字段里如果有逗号或双引号，就用双引号包起来，
    /// 字段内部的双引号要写成两个双引号（CSV 标准规则）。
    /// </summary>
    private static string Escape(string value)
        => value.Contains(',') || value.Contains('"')
            ? '"' + value.Replace("\"", "\"\"") + '"'
            : value;

    /// <summary>
    /// 自己解析一行 CSV：逐字符扫描，遇到逗号就切列；
    /// 引号内的逗号不算分隔符，连续两个引号表示一个真正的引号。
    /// 只支持单行字段（本程序写出的数据不含换行，够了）。
    /// </summary>
    private static List<string> SplitCsv(string line)
    {
        var cells = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // "" 表示转义后的一个 "
                    sb.Append('"');
                    i++; // 跳过下一个引号
                }
                else if (c == '"') inQuotes = false; // 引号结束
                else sb.Append(c);
            }
            else if (c == '"') inQuotes = true;
            else if (c == ',') { cells.Add(sb.ToString()); sb.Clear(); }
            else sb.Append(c);
        }

        // 最后一列没有逗号收尾，循环结束后要补上
        cells.Add(sb.ToString());
        return cells;
    }
}
