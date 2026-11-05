# 09 数据存储：CSV、SQLite、SQL Server

> 本章目标：会设计上位机的数据表、会写批量插入、会做历史查询和清理。
> 数据是上位机最值钱的部分——设备可以重启，历史数据丢了就是事故。

---

## 1. 上位机要存哪些数据

| 数据 | 特点 | 存储建议 |
|---|---|---|
| 实时值 | 每秒变化、只关心最新 | 内存（字典） |
| 历史数据 | 时序、量大、要查询 | SQLite / SQL Server / 时序库 |
| 报警记录 | 事件型、要追溯 | 数据库表 |
| 操作日志 | 审计、不可改 | 数据库表（只增） |
| 配方/参数 | 结构化、要版本 | 数据库表 + JSON |
| 用户/权限 | 小、要安全 | 数据库表（密码哈希） |
| 配置文件 | 少量、人可读 | JSON 文件 |

---

## 2. 表设计（可直接用）

```sql
-- 设备表
CREATE TABLE Devices (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    Name        TEXT NOT NULL,
    Protocol    TEXT NOT NULL,          -- ModbusTcp / ModbusRtu / S7
    Endpoint    TEXT NOT NULL,          -- 192.168.1.10:502
    SlaveId     INTEGER,
    Enabled     INTEGER DEFAULT 1
);

-- 点位表
CREATE TABLE Tags (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    DeviceId    INTEGER NOT NULL REFERENCES Devices(Id),
    Name        TEXT NOT NULL,
    Address     INTEGER NOT NULL,
    DataType    TEXT NOT NULL,          -- Int16 / UInt16 / Float32 ...
    WordOrder   TEXT DEFAULT 'Abcd',
    Scale       REAL DEFAULT 1,
    OffsetValue REAL DEFAULT 0,
    Unit        TEXT,
    LowLimit    REAL,
    HighLimit   REAL
);

-- 历史数据（核心表，量大）
CREATE TABLE History (
    Id       INTEGER PRIMARY KEY AUTOINCREMENT,
    TagId    INTEGER NOT NULL,
    Ts       TEXT NOT NULL,             -- ISO8601 或 'yyyy-MM-dd HH:mm:ss.fff'
    Value    REAL NOT NULL,
    Quality  INTEGER DEFAULT 0          -- 0=Good 1=Uncertain 2=Bad
);
CREATE INDEX IX_History_Tag_Ts ON History(TagId, Ts);

-- 报警
CREATE TABLE Alarms (
    Id        INTEGER PRIMARY KEY AUTOINCREMENT,
    TagId     INTEGER NOT NULL,
    Ts        TEXT NOT NULL,
    Level     TEXT NOT NULL,            -- High / Low / Fault
    Value     REAL,
    Message   TEXT,
    AckUser   TEXT,
    AckTime   TEXT
);

-- 操作审计
CREATE TABLE AuditLog (
    Id       INTEGER PRIMARY KEY AUTOINCREMENT,
    Ts       TEXT NOT NULL,
    UserName TEXT NOT NULL,
    Action   TEXT NOT NULL,             -- WriteTag / StartDevice ...
    Target   TEXT,
    OldValue TEXT,
    NewValue TEXT
);

-- 用户
CREATE TABLE Users (
    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
    UserName     TEXT UNIQUE NOT NULL,
    PasswordHash TEXT NOT NULL,         -- PBKDF2/BCrypt，绝不存明文
    Role         TEXT NOT NULL,         -- Operator / Engineer / Admin
    Enabled      INTEGER DEFAULT 1
);
```

设计要点：

1. **历史表只增不改**，查询用索引 `(TagId, Ts)`。
2. **时间统一存 UTC 或明确本地时区**，展示时再转换（跨时区项目必备）。
3. **Quality 字段不能省**，否则分不清“0℃”和“没采到”。
4. 点位地址等元数据存表，不要写死在代码里。
5. 大量删除用“按天分表/分区”，不要 `DELETE FROM History WHERE ...` 慢慢磨。

---

## 3. 选型对比

| 方案 | 容量 | 查询 | 部署 | 适用 |
|---|---|---|---|---|
| CSV 文件 | 百万行级 | 只能顺序读 | 零 | 单机小项目、导出、排障 |
| SQLite | 千万行级 | SQL、索引 | 零（单文件） | **单机上位机首选** |
| SQL Server Express | 10 GB | 强 | 要装服务 | 多客户端、局域网 |
| MySQL/PostgreSQL | 大 | 强 | 要装服务 | 服务器/多站点 |
| InfluxDB/TDengine | 海量 | 时序优化 | 要装服务 | 高频采集、长期存储 |

> 经验：**单机先 SQLite，多台电脑看同一份数据再上 SQL Server。**

---

## 4. SQLite 实战（C#）

安装：

```powershell
dotnet add package Microsoft.Data.Sqlite --version 8.0.8
```

连接与建表：

```csharp
using Microsoft.Data.Sqlite;

var connectionString = "Data Source=data/history.db;Cache=Shared";
using var conn = new SqliteConnection(connectionString);
conn.Open();

using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = """
        PRAGMA journal_mode=WAL;          -- 读写并发更好，断电更安全
        CREATE TABLE IF NOT EXISTS History(
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            TagId INTEGER NOT NULL,
            Ts TEXT NOT NULL,
            Value REAL NOT NULL,
            Quality INTEGER NOT NULL DEFAULT 0);
        CREATE INDEX IF NOT EXISTS IX_History_Tag_Ts ON History(TagId, Ts);
        """;
    cmd.ExecuteNonQuery();
}
```

批量插入必须用事务 + 参数化：

```csharp
using var tx = conn.BeginTransaction();
using var cmd = conn.CreateCommand();
cmd.Transaction = tx;
cmd.CommandText = "INSERT INTO History(TagId, Ts, Value, Quality) VALUES($tag, $ts, $value, $q)";

var pTag = cmd.Parameters.Add("$tag", SqliteType.Integer);
var pTs = cmd.Parameters.Add("$ts", SqliteType.Text);
var pValue = cmd.Parameters.Add("$value", SqliteType.Real);
var pQ = cmd.Parameters.Add("$q", SqliteType.Integer);

foreach (var r in records)
{
    pTag.Value = r.TagId;
    pTs.Value = r.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
    pValue.Value = r.Value;
    pQ.Value = (int)r.Quality;
    cmd.ExecuteNonQuery();
}
tx.Commit();
```

**性能对比**（10 万条插入）：

| 写法 | 耗时 |
|---|---|
| 每条自动提交 | 60~120 s |
| 单事务 | 1~3 s |
| 单事务 + 预编译参数 | 0.5~1.5 s |

查询：

```csharp
using var cmd = conn.CreateCommand();
cmd.CommandText = """
    SELECT Ts, Value FROM History
    WHERE TagId = $tag AND Ts BETWEEN $from AND $to
    ORDER BY Ts LIMIT 5000
    """;
cmd.Parameters.AddWithValue("$tag", tagId);
cmd.Parameters.AddWithValue("$from", "2026-10-18 00:00:00");
cmd.Parameters.AddWithValue("$to", "2026-10-18 23:59:59");

using var reader = cmd.ExecuteReader();
while (reader.Read())
{
    var ts = reader.GetString(0);
    var value = reader.GetDouble(1);
}
```

清理旧数据：

```sql
DELETE FROM History WHERE Ts < '2026-09-18 00:00:00';
VACUUM;   -- 回收空间，低峰期执行
```

---

## 5. EF Core（可选）

适合 CRUD 多的业务表（用户、配方），**不适合高频历史写入**。

```powershell
dotnet add package Microsoft.EntityFrameworkCore.Sqlite --version 8.0.8
```

```csharp
public sealed class AppDbContext : DbContext
{
    public DbSet<HistoryRow> History => Set<HistoryRow>();
    protected override void OnConfiguring(DbContextOptionsBuilder options) =>
        options.UseSqlite("Data Source=data/history.db");
}

await using var db = new AppDbContext();
db.History.AddRange(rows);
await db.SaveChangesAsync();
```

历史数据用 `ExecuteSqlRaw` 批量插，或干脆用 ADO.NET。

---

## 6. SQL Server 要点

连接串：

```
Server=192.168.1.5;Database=UpperComputer;User Id=sa;Password=***;TrustServerCertificate=True;
```

- 批量插入用 `SqlBulkCopy`（10 万条几秒）。
- 大表按天分区，或每月一张表。
- 定期做索引重建/统计更新。
- 生产环境别用 sa，建专用账号，最小权限。

---

## 7. 时序数据优化：死区与降采样

### 7.1 死区（Deadband）

值没怎么变就不存，能省 80% 空间：

```csharp
if (Math.Abs(value - lastStored) < deadband) return;   // 例如温度 0.2℃
```

### 7.2 降采样

查一个月趋势时，不要返回 100 万点，按分钟取平均：

```sql
SELECT strftime('%Y-%m-%d %H:%M', Ts) AS Minute, AVG(Value)
FROM History
WHERE TagId = 1 AND Ts >= '2026-10-01'
GROUP BY Minute
ORDER BY Minute;
```

### 7.3 保留策略

- 原始数据保留 3~6 个月；
- 分钟均值保留 2~3 年；
- 班报/日报永久保留；
- 到期自动归档/删除。

---

## 8. 本仓库的 CSV 实现

`Industrial.Core/Storage/CsvHistoryStore.cs`：

- 按天一个文件 `data/20261018.csv`；
- 表头：时间,设备,位号,数值,单位,质量,报警；
- 批量写入 + 每次 flush（断电最多丢最后一批）；
- 带 BOM，Excel 打开不乱码；
- `Cleanup(now, keepDays)` 自动删旧文件；
- 内部 `lock` 保证多线程安全；
- 时间/数值用 `InvariantCulture`。

为什么先用 CSV：

1. 零依赖，任何机器能跑；
2. 现场排障时直接 Excel 打开；
3. 数据量小时比数据库省事。

什么时候必须换数据库：

- 单文件超过 100 MB；
- 需要按条件查询/统计；
- 多个客户端同时读；
- 需要事务和并发写。

替换路径：

```csharp
public interface IHistoryStore
{
    void Append(IEnumerable<HistoryRecord> records);
    IReadOnlyList<HistoryRecord> Query(int tagId, DateTime from, DateTime to);
}
```

实现 `CsvHistoryStore` 和 `SqliteHistoryStore`，配置里切换——**上层代码一行不改**。

---

## 9. 数据安全

| 风险 | 措施 |
|---|---|
| 断电丢数据 | WAL 模式、事务、定期备份 |
| 磁盘满 | 监控剩余空间、自动清理、写入失败告警 |
| 文件被占用 | 读写用 `FileShare.Read`，别锁死 |
| 数据被篡改 | 审计表只增不删、数据库账号权限最小化 |
| 密码泄露 | 哈希存储（PBKDF2/BCrypt）+ 加盐 |
| 备份丢失 | 每天自动备份到另一个盘/NAS |

---

## 10. 本章验收

- [ ] 能画出历史/报警/审计三张表并写出建表 SQL
- [ ] 能用 SQLite 在 3 秒内插入 10 万条数据
- [ ] 能写按时间范围 + 位号的查询
- [ ] 能解释死区和降采样的作用
- [ ] 能设计清理策略（保留多久、怎么删）
- [ ] 把本仓库的 CSV 存储替换成 SQLite（进阶任务）

下一章：`10 多线程、定时采集与线程安全.md`
