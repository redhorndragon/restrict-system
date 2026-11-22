using System.Security.Principal;
using System.Text.Json;
using Microsoft.Win32;

namespace KioskSetup;

internal static class Program
{
    private const string ServiceName = "KioskGuard";
    private const string ProgramDataDir = @"C:\ProgramData\KioskGuard";
    private const string ShellValueName = "Shell";
    private const string WinlogonKeyPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon";

    private static int Main(string[] args)
    {
        if (!IsAdministrator())
        {
            Console.Error.WriteLine("请以管理员身份运行。");
            return 1;
        }

        if (!TryParse(args, out var options, out var error))
        {
            Console.Error.WriteLine(error);
            PrintUsage();
            return 1;
        }

        try
        {
            var sid = ResolveSid(options.UserName);
            var installDir = Path.GetDirectoryName(options.LauncherPath) ?? Environment.CurrentDirectory;

            WriteConfig(installDir, options, sid);
            ConfigureShell(sid, options.UserName, options.LauncherPath);
            ProtectDirectory(installDir);
            ProtectDirectory(ProgramDataDir);
            InstallService(options.ServicePath);

            Console.WriteLine($"配置完成：用户={options.UserName}，SID={sid}");
            Console.WriteLine("请重启并使该用户登录以生效。");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"配置失败：{ex.Message}");
            return 1;
        }
    }

    private static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static SecurityIdentifier ResolveSid(string userName)
    {
        var accountName = userName.Contains('\\') || userName.Contains('@')
            ? userName
            : $@"{Environment.MachineName}\{userName}";
        var account = new NTAccount(accountName);
        return (SecurityIdentifier)account.Translate(typeof(SecurityIdentifier));
    }

    private static void WriteConfig(string installDir, Options options, SecurityIdentifier sid)
    {
        Directory.CreateDirectory(ProgramDataDir);
        var config = new
        {
            options.GitHubPagesUrl,
            TargetUserSid = sid.Value,
            TargetUserName = options.UserName,
            options.StartUrl,
            options.RefreshIntervalMinutes,
            ProcessScanIntervalMs = 500,
            CachePath = Path.Combine(ProgramDataDir, "allowlist.json"),
            SharedConfigPath = Path.Combine(ProgramDataDir, "config.json"),
            options.ManifestScriptId,
            options.AllowlistContainerId,
            AllowedProcessNames = new[]
            {
                "msedge.exe", "msedgewebview2.exe", "KioskLauncher.exe",
                "conhost.exe", "dllhost.exe", "RuntimeBroker.exe", "ctfmon.exe",
                "sihost.exe", "TextInputHost.exe", "ApplicationFrameHost.exe",
                "SearchHost.exe", "SearchApp.exe", "audiodg.exe", "fontdrvhost.exe",
            },
            EdgePath = options.EdgePath,
        };
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(ProgramDataDir, "config.json"), json);

        var launcherConfig = new
        {
            options.StartUrl,
            options.EdgePath,
            SharedConfigPath = Path.Combine(ProgramDataDir, "config.json"),
            RestartDelayMs = 1500,
        };
        File.WriteAllText(Path.Combine(installDir, "launcher.json"),
            JsonSerializer.Serialize(launcherConfig, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static void ConfigureShell(SecurityIdentifier sid, string userName, string launcherPath)
    {
        const string hiveName = "KioskGuardTempHive";
        var loadedByUs = false;
        try
        {
            if (Registry.Users.OpenSubKey(sid.Value) == null)
            {
                var profilePath = EnsureProfile(sid, userName);
                var ntuser = Path.Combine(profilePath, "NTUSER.DAT");
                if (!File.Exists(ntuser)) throw new InvalidOperationException($"未找到用户配置单元：{ntuser}");
                RunProcess("reg.exe", $"load HKU\\{hiveName} \"{ntuser}\"");
                loadedByUs = true;
            }

            var hiveSid = loadedByUs ? hiveName : sid.Value;
            using var key = Registry.Users.OpenSubKey($@"{hiveSid}\{WinlogonKeyPath}", true)
                            ?? throw new InvalidOperationException("无法打开目标用户 Winlogon 注册表项。");

            var backupPath = Path.Combine(ProgramDataDir, "shell-backup.txt");
            if (!File.Exists(backupPath))
            {
                File.WriteAllText(backupPath, key.GetValue(ShellValueName)?.ToString() ?? "explorer.exe");
            }
            key.SetValue(ShellValueName, $"\"{launcherPath}\"", RegistryValueKind.String);
        }
        finally
        {
            if (loadedByUs) RunProcess("reg.exe", $"unload HKU\\{hiveName}");
        }
    }

    private static string EnsureProfile(SecurityIdentifier sid, string userName)
    {
        var profileKeyPath = $@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList\{sid.Value}";
        using var existing = Registry.LocalMachine.OpenSubKey(profileKeyPath);
        if (existing != null)
        {
            var path = existing.GetValue("ProfileImagePath")?.ToString();
            if (!string.IsNullOrEmpty(path)) return path;
        }

        var profilePath = Path.Combine(@"C:\Users", Path.GetFileName(userName.Replace('\\', '/').TrimEnd('/')));
        Directory.CreateDirectory(profilePath);

        var defaultNtuser = @"C:\Users\Default\NTUSER.DAT";
        var ntuser = Path.Combine(profilePath, "NTUSER.DAT");
        if (!File.Exists(ntuser) && File.Exists(defaultNtuser))
        {
            File.Copy(defaultNtuser, ntuser);
        }

        using var key = Registry.LocalMachine.CreateSubKey(profileKeyPath, true)
                        ?? throw new InvalidOperationException("无法创建用户 ProfileList。");
        key.SetValue("ProfileImagePath", profilePath, RegistryValueKind.ExpandString);
        key.SetValue("Flags", 0, RegistryValueKind.DWord);
        key.SetValue("State", 0, RegistryValueKind.DWord);

        Console.WriteLine($"已为用户创建配置骨架：{profilePath}");
        return profilePath;
    }

    private static void ProtectDirectory(string path)
    {
        if (!Directory.Exists(path)) return;
        // ACL 收紧由 install.ps1 处理，这里仅确保目录存在
    }

    private static void InstallService(string servicePath)
    {
        var query = RunProcess("sc.exe", $"query {ServiceName}");
        if (!query.Contains("不存在", StringComparison.OrdinalIgnoreCase) &&
            !query.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            RunProcess("sc.exe", $"stop {ServiceName}");
            RunProcess("sc.exe", $"delete {ServiceName}");
        }
        RunProcess("sc.exe", $"create {ServiceName} binPath= \"{servicePath}\" start= auto DisplayName= \"KioskGuard Service\"");
        RunProcess("sc.exe", $"failure {ServiceName} reset= 86400 actions= restart/60000/restart/60000/restart/60000");
        RunProcess("sc.exe", $"start {ServiceName}");
    }

    private static string RunProcess(string file, string args)
    {
        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(file, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        }) ?? throw new InvalidOperationException($"无法启动进程：{file}");
        var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0 && !output.Contains("已停止", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"[sc] {args}: {output.Trim()}");
        }
        return output;
    }

    private static bool TryParse(string[] args, out Options options, out string error)
    {
        options = new Options();
        error = string.Empty;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--user": options.UserName = Next(args, ref i); break;
                case "--github": options.GitHubPagesUrl = Next(args, ref i); break;
                case "--start": options.StartUrl = Next(args, ref i); break;
                case "--launcher": options.LauncherPath = Next(args, ref i); break;
                case "--service": options.ServicePath = Next(args, ref i); break;
                case "--refresh": int.TryParse(Next(args, ref i), out var refresh); options.RefreshIntervalMinutes = refresh > 0 ? refresh : 10; break;
                case "--manifest-id": options.ManifestScriptId = Next(args, ref i); break;
                case "--container-id": options.AllowlistContainerId = Next(args, ref i); break;
                case "--edge": options.EdgePath = Next(args, ref i); break;
                default: error = $"未知参数：{args[i]}"; return false;
            }
        }

        if (string.IsNullOrWhiteSpace(options.UserName)) { error = "缺少 --user"; return false; }
        if (string.IsNullOrWhiteSpace(options.GitHubPagesUrl)) { error = "缺少 --github"; return false; }
        if (string.IsNullOrWhiteSpace(options.StartUrl)) options.StartUrl = options.GitHubPagesUrl;
        if (string.IsNullOrWhiteSpace(options.LauncherPath)) { error = "缺少 --launcher"; return false; }
        if (string.IsNullOrWhiteSpace(options.ServicePath)) { error = "缺少 --service"; return false; }
        return true;
    }

    private static string Next(string[] args, ref int i)
    {
        if (++i >= args.Length) throw new ArgumentException($"参数 {args[i - 1]} 缺少值");
        return args[i];
    }

    private static void PrintUsage()
    {
        Console.WriteLine("用法：KioskSetup --user <用户名> --github <GitHub Pages URL> [选项]");
        Console.WriteLine("  --start <URL>              Edge 起始页（默认同 --github）");
        Console.WriteLine("  --launcher <path>          KioskLauncher.exe 完整路径");
        Console.WriteLine("  --service <path>           KioskGuard.Service.exe 完整路径");
        Console.WriteLine("  --refresh <分钟>           白名单刷新周期（默认 10）");
        Console.WriteLine("  --manifest-id <id>         JSON manifest script 的 id（默认 kiosk-allowlist）");
        Console.WriteLine("  --container-id <id>        链接容器元素 id（默认 kiosk-allowlist）");
        Console.WriteLine("  --edge <path>              msedge.exe 路径（可选，自动探测）");
    }

    private sealed class Options
    {
        public string UserName { get; set; } = string.Empty;
        public string GitHubPagesUrl { get; set; } = string.Empty;
        public string StartUrl { get; set; } = string.Empty;
        public string LauncherPath { get; set; } = string.Empty;
        public string ServicePath { get; set; } = string.Empty;
        public string EdgePath { get; set; } = string.Empty;
        public int RefreshIntervalMinutes { get; set; } = 10;
        public string ManifestScriptId { get; set; } = "kiosk-allowlist";
        public string AllowlistContainerId { get; set; } = "kiosk-allowlist";
    }
}
