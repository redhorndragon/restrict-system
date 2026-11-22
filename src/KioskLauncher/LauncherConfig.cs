using System.Text.Json;

namespace KioskLauncher;

public class LauncherConfig
{
    public string StartUrl { get; set; } = string.Empty;
    public string SharedConfigPath { get; set; } = @"C:\ProgramData\KioskGuard\config.json";
    public string BrowserPolicyPath { get; set; } = @"C:\ProgramData\KioskGuard\browser-policy.json";

    public static LauncherConfig Load()
    {
        var userConfig = Path.Combine(AppContext.BaseDirectory, "launcher.json");
        var path = File.Exists(userConfig) ? userConfig : @"C:\ProgramData\KioskGuard\config.json";
        try
        {
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<LauncherConfig>(json) ?? new LauncherConfig();
            }
        }
        catch { }
        return new LauncherConfig();
    }
}
