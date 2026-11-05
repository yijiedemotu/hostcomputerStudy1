using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Industrial.Core.Storage;

/// <summary>
/// 配置读写（JSON）。上位机的“配方/点位表/通信参数”都应该放配置文件，
/// 不要写死在代码里——现场改一个地址就要重新编译发包，是新手最常犯的错。
/// </summary>
public static class JsonConfigStore
{
    /// <summary>
    /// 全局序列化设置：
    /// 缩进输出、属性名不区分大小写、允许注释和尾逗号（手改配置更方便）、
    /// 枚举写成字符串（"ModbusTcp" 比 0 好懂）、中文不转义。
    /// </summary>
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        // 中文不转义成 \uXXXX，配置文件人能看懂
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// 读取配置。文件不存在就用 defaultFactory 生成一份默认配置并写盘，
    /// 保证程序第一次运行也能直接启动。
    /// </summary>
    public static T Load<T>(string path, Func<T> defaultFactory) where T : class
    {
        if (!File.Exists(path))
        {
            var created = defaultFactory();
            Save(path, created);
            return created;
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json, Options)
                   ?? throw new InvalidDataException($"配置文件 {path} 反序列化结果为 null");
        }
        catch (JsonException ex)
        {
            // 配置坏了不能让程序起不来：备份坏文件，方便事后排查。
            var backup = path + $".bad-{DateTime.Now:yyyyMMddHHmmss}";
            try { File.Copy(path, backup, overwrite: true); } catch { }
            throw new InvalidDataException($"配置文件 {path} 格式错误，已备份为 {backup}：{ex.Message}", ex);
        }
    }

    /// <summary>保存配置。先写临时文件再原子替换，防止写到一半断电留下半个 JSON。</summary>
    public static void Save<T>(string path, T value)
    {
        // 确保上级目录存在
        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        // 先写临时文件再替换，避免写一半断电把配置弄坏。
        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(value, Options));
        File.Move(temp, path, overwrite: true);
    }
}
