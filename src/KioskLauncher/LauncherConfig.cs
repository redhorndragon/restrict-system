using System.Text.Json;

namespace KioskLauncher;

public class LauncherConfig
{
    public string StartUrl { get; set; } = string.Empty;
    public string EdgePath { get; set; } = string.Empty;
    public string SharedConfigPath { get; set; } = @"C:\ProgramData\KioskGuard\config.json";
    public int RestartDelayMs { get; set; } = 1500;

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
