using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace KioskGuard.Config;

/// <summary>
/// 自研浏览器使用的白名单策略文件。由服务写入，浏览器以只读方式加载。
/// </summary>
public sealed class BrowserPolicy
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
    };

    public int Version { get; set; } = 1;
    public DateTimeOffset FetchedAt { get; set; }
    public string? SourceUrl { get; set; }
    public string StartUrl { get; set; } = string.Empty;
    public List<string> AllowedUrls { get; set; } = [];
    public List<string> BilibiliUpIds { get; set; } = [];

    public static BrowserPolicy? Load(string path)
    {
        if (!File.Exists(path)) return null;
        try
        {
            return JsonSerializer.Deserialize<BrowserPolicy>(File.ReadAllText(path), Options);
        }
        catch
        {
            return null;
        }
    }

    public void Save(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        var temp = path + ".tmp." + Guid.NewGuid().ToString("N");
        try
        {
            File.WriteAllText(temp, JsonSerializer.Serialize(this, Options));
            File.Move(temp, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temp))
            {
                try { File.Delete(temp); } catch { }
            }
        }
    }
}
