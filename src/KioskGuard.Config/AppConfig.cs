namespace KioskGuard.Config;

public class AppConfig
{
    public string GitHubPagesUrl { get; set; } = string.Empty;
    public string TargetUserSid { get; set; } = string.Empty;
    public string TargetUserName { get; set; } = string.Empty;
    public string StartUrl { get; set; } = string.Empty;
    public int RefreshIntervalMinutes { get; set; } = 10;
    public int ProcessScanIntervalMs { get; set; } = 500;
    public string CachePath { get; set; } = @"C:\ProgramData\KioskGuard\allowlist.json";
    public string SharedConfigPath { get; set; } = @"C:\ProgramData\KioskGuard\config.json";
    public string ManifestScriptId { get; set; } = "kiosk-allowlist";
    public string AllowlistContainerId { get; set; } = "kiosk-allowlist";
    public List<string> ExtraAllowedUrls { get; set; } = [];
    public List<string> AllowedProcessNames { get; set; } =
    [
        "msedge.exe",
        "msedgewebview2.exe",
        "KioskLauncher.exe",
        "conhost.exe",
        "dllhost.exe",
        "RuntimeBroker.exe",
        "ctfmon.exe",
        "sihost.exe",
        "TextInputHost.exe",
        "ApplicationFrameHost.exe",
        "SearchHost.exe",
        "SearchApp.exe",
        "audiodg.exe",
        "fontdrvhost.exe",
        "wlanext.exe",
    ];
}
