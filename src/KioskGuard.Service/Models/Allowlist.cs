using System.Text.Json;

namespace KioskGuard.Service.Models;

public class Allowlist
{
    public DateTimeOffset FetchedAt { get; set; }
    public string? SourceUrl { get; set; }
    public List<string> Urls { get; set; } = [];
    public List<string> BilibiliUpIds { get; set; } = [];

    public static Allowlist? Load(string path)
    {
        if (!File.Exists(path)) return null;
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Allowlist>(json);
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
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }
}
