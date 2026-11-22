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
        AppendLog($"启动：StartUrl={config.StartUrl}");
        using var hook = new KeyboardHook();
        using var form = new KioskForm();
        using var edge = new EdgeController(config, msg => AppendLog(msg));

        var started = edge.Start();
        AppendLog($"Edge.Start 返回 {started}");
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
            var publicDir = @"C:\Users\Public\Documents\KioskGuard";
            Directory.CreateDirectory(publicDir);
            File.AppendAllText(Path.Combine(publicDir, "launcher.log"), line);
        }
        catch { }
    }
}
