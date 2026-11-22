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
        using var hook = new KeyboardHook();
        using var form = new KioskForm();
        using var edge = new EdgeController(config, msg => AppendLog(msg));

        edge.Start();
        Application.Run(form);
    }

    private static void AppendLog(string message)
    {
        try
        {
            var dir = Path.Combine(Path.GetTempPath(), "KioskGuard");
            Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "launcher.log"),
                $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
        }
        catch { }
    }
}
