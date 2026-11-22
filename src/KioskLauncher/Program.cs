namespace KioskLauncher;

static class Program
{
    private static readonly Mutex SingleInstance = new(true, "Local\\KioskLauncher_SingleInstance");

    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        if (!SingleInstance.WaitOne(TimeSpan.Zero, true))
        {
            return;
        }

        var config = LauncherConfig.Load();
        AppendLog($"启动：StartUrl={config.StartUrl} Policy={config.BrowserPolicyPath}");

        using var hook = new KeyboardHook();
        var policyStore = new BrowserPolicyStore(config.BrowserPolicyPath, config.StartUrl);
        using var form = new BrowserForm(policyStore, config.StartUrl, AppendLog);
        Application.Run(form);
    }

    private static void AppendLog(string message)
    {
        var line = $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
        try
        {
            var dir = Path.Combine(Path.GetTempPath(), "KioskGuard");
            Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "launcher.log"), line);
        }
        catch { }
        try
        {
            const string publicDir = @"C:\Users\Public\Documents\KioskGuard";
            Directory.CreateDirectory(publicDir);
            File.AppendAllText(Path.Combine(publicDir, "launcher.log"), line);
        }
        catch { }
    }
}
