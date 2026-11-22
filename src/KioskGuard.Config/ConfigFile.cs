using System.Text.Json;
using System.Text.Json.Nodes;

namespace KioskGuard.Config;

public static class ConfigFile
{
    public static AppConfig Load(string path)
    {
        if (!File.Exists(path)) return new AppConfig();
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
    }

    public static ValidationResult Validate(string githubPagesUrl, string startUrl, int refreshMinutes)
    {
        var errors = new Dictionary<string, string>();

        if (!IsHttpUrl(githubPagesUrl))
            errors[nameof(AppConfig.GitHubPagesUrl)] = "请输入有效的 http/https 地址。";
        if (!IsHttpUrl(startUrl))
            errors[nameof(AppConfig.StartUrl)] = "请输入有效的 http/https 地址。";
        if (refreshMinutes < 1 || refreshMinutes > 1440)
            errors[nameof(AppConfig.RefreshIntervalMinutes)] = "刷新周期须在 1 至 1440 分钟之间。";

        return new ValidationResult(errors.Count == 0, errors);
    }

    public static void SaveWhitelistSource(string configPath, string githubPagesUrl, string startUrl, int refreshMinutes)
    {
        var result = Validate(githubPagesUrl, startUrl, refreshMinutes);
        if (!result.IsValid)
            throw new ArgumentException(string.Join("；", result.Errors.Values));

        var root = File.Exists(configPath)
            ? JsonNode.Parse(File.ReadAllText(configPath)) as JsonObject ?? new JsonObject()
            : new JsonObject();

        root[nameof(AppConfig.GitHubPagesUrl)] = githubPagesUrl;
        root[nameof(AppConfig.StartUrl)] = startUrl;
        root[nameof(AppConfig.RefreshIntervalMinutes)] = refreshMinutes;

        AtomicWrite(configPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    public static void SaveLauncherStartUrl(string launcherPath, string startUrl)
    {
        if (!IsHttpUrl(startUrl)) throw new ArgumentException("起始页不是有效的 http/https 地址。");
        if (!File.Exists(launcherPath)) return;

        var root = JsonNode.Parse(File.ReadAllText(launcherPath)) as JsonObject ?? new JsonObject();
        root[nameof(AppConfig.StartUrl)] = startUrl;
        AtomicWrite(launcherPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    private static bool IsHttpUrl(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static void AtomicWrite(string path, string content)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        var temp = path + ".tmp." + Guid.NewGuid().ToString("N");
        try
        {
            File.WriteAllText(temp, content);
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

public sealed record ValidationResult(bool IsValid, IReadOnlyDictionary<string, string> Errors);
