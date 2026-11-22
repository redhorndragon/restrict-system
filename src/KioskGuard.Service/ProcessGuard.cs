using System.Management;
using System.Diagnostics;
using KioskGuard.Config;

namespace KioskGuard.Service;

public sealed class ProcessGuard : IDisposable
{
    private static readonly HashSet<string> ProtectedProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "userinit.exe", "explorer.exe", "dwm.exe", "winlogon.exe", "csrss.exe",
        "smss.exe", "wininit.exe", "services.exe", "lsass.exe", "svchost.exe",
        "taskhostw.exe", "ShellHost.exe", "uihost.exe", "sihost.exe",
        "RuntimeBroker.exe", "ApplicationFrameHost.exe", "SearchHost.exe",
        "SearchApp.exe", "TextInputHost.exe", "ctfmon.exe", "conhost.exe",
        "dllhost.exe", "audiodg.exe", "fontdrvhost.exe", "ChsIME.exe",
        "CrossDeviceResume.exe", "nvcontainer.exe", "nvnodejslauncher.exe",
        "igfxEMN.exe", "opushutil.exe", "spoolsv.exe", "MsMpEng.exe",
        "SecurityHealthService.exe", "SearchIndexer.exe",
    };

    private readonly ConfigStore _store;
    private readonly ILogger<ProcessGuard> _logger;
    private readonly HashSet<string> _allowed;
    private readonly Dictionary<string, DateTimeOffset> _lastLog = new(StringComparer.OrdinalIgnoreCase);
    private ManagementEventWatcher? _startWatcher;
    private PeriodicTimer? _scanTimer;
    private Task? _scanTask;
    private bool _disposed;

    public ProcessGuard(ConfigStore store, ILogger<ProcessGuard> logger)
    {
        _store = store;
        _logger = logger;
        _allowed = new HashSet<string>(store.Current.AllowedProcessNames, StringComparer.OrdinalIgnoreCase);
    }

    public Task StartAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_store.Current.TargetUserSid))
        {
            _logger.LogWarning("TargetUserSid 未配置，进程守护未启动。");
            return Task.CompletedTask;
        }

        try
        {
            var query = new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace");
            _startWatcher = new ManagementEventWatcher(query);
            _startWatcher.EventArrived += OnProcessStart;
            _startWatcher.Start();
            _logger.LogInformation("进程启动监听已注册。");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "注册 WMI 进程启动监听失败，将仅依赖定时扫描。");
        }

        _scanTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(Math.Max(500, _store.Current.ProcessScanIntervalMs)));
        _scanTask = Task.Run(() => ScanLoopAsync(ct), ct);
        return Task.CompletedTask;
    }

    private async Task ScanLoopAsync(CancellationToken ct)
    {
        try
        {
            while (await _scanTimer!.WaitForNextTickAsync(ct))
            {
                ScanAll();
            }
        }
        catch (OperationCanceledException) { }
    }

    private void ScanAll()
    {
        try
        {
            foreach (var process in Process.GetProcesses())
            {
                using (process)
                {
                    try
                    {
                        if (process.HasExited) continue;
                        if (!IsTargetUser(process.Id)) continue;
                        Enforce(process.Id, SafeProcessName(process));
                    }
                    catch { }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "进程扫描异常。");
        }
    }

    private void OnProcessStart(object sender, EventArrivedEventArgs e)
    {
        try
        {
            var pid = Convert.ToInt32(e.NewEvent["ProcessID"]);
            var name = e.NewEvent["ProcessName"]?.ToString() ?? string.Empty;
            if (!IsTargetUser(pid)) return;
            Enforce(pid, name);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "处理进程启动事件异常。");
        }
    }

    private void Enforce(int pid, string processName)
    {
        if (string.IsNullOrEmpty(processName)) return;
        var fileName = Path.GetFileName(processName);
        if (_allowed.Contains(fileName) || ProtectedProcesses.Contains(fileName)) return;

        try
        {
            var process = Process.GetProcessById(pid);
            using (process)
            {
                process.Kill();
                LogTermination(fileName);
            }
        }
        catch (ArgumentException) { }
        catch (InvalidOperationException) { }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "终止进程 {Name} 失败。", processName);
        }
    }

    private void LogTermination(string processName)
    {
        var key = Path.GetFileName(processName);
        var now = DateTimeOffset.UtcNow;
        if (_lastLog.TryGetValue(key, out var last) && (now - last) < TimeSpan.FromSeconds(30)) return;
        _lastLog[key] = now;
        _logger.LogWarning("已终止目标用户进程：{Name}", key);
    }

    private bool IsTargetUser(int pid)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher($"SELECT __PATH FROM Win32_Process WHERE ProcessId = {pid}");
            using var collection = searcher.Get();
            foreach (ManagementObject obj in collection)
            {
                using (obj)
                {
                    using var invoker = new ManagementClass("Win32_Process");
                    // GetOwnerSid 为静态方法，需通过实例调用
                    var inParams = invoker.GetMethodParameters("GetOwnerSid");
                    using var result = obj.InvokeMethod("GetOwnerSid", inParams, null);
                    var sid = result?["Sid"]?.ToString();
                    return string.Equals(sid, _store.Current.TargetUserSid, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
        catch { }
        return false;
    }

    private static string SafeProcessName(Process process)
    {
        try { return process.ProcessName + ".exe"; }
        catch { return string.Empty; }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_startWatcher != null)
        {
            _startWatcher.EventArrived -= OnProcessStart;
            _startWatcher.Stop();
            _startWatcher.Dispose();
        }
        _scanTimer?.Dispose();
        _scanTask?.Wait(TimeSpan.FromSeconds(2));
    }
}
