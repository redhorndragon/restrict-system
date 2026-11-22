using System.Diagnostics;

namespace KioskLauncher;

public sealed class EdgeController : IDisposable
{
    private readonly LauncherConfig _config;
    private readonly Action<string> _log;
    private Process? _edge;
    private CancellationTokenSource? _watchCts;
    private bool _disposed;
    private string _resolvedPath = string.Empty;

    public EdgeController(LauncherConfig config, Action<string> log)
    {
        _config = config;
        _log = log;
    }

    public bool Start()
    {
        var path = ResolveEdgePath();
        if (path == null)
        {
            _log("未找到 Microsoft Edge，无法启动。");
            return false;
        }
        _resolvedPath = path;

        if (string.IsNullOrWhiteSpace(_config.StartUrl))
        {
            _log("StartUrl 未配置，无法启动 Edge。");
            return false;
        }

        Launch();
        _watchCts = new CancellationTokenSource();
        _ = WatchAsync(_watchCts.Token);
        return true;
    }

    private void Launch()
    {
        try
        {
            var psi = new ProcessStartInfo(_resolvedPath)
            {
                UseShellExecute = false,
                Arguments = BuildArguments(_config.StartUrl),
            };
            _edge = Process.Start(psi);
            _log("Edge 已启动。");
        }
        catch (Exception ex)
        {
            _log($"启动 Edge 失败：{ex.Message}");
        }
    }

    private async Task WatchAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(1000, ct);
                if (_edge == null || _edge.HasExited)
                {
                    _log("Edge 已退出，正在重启。");
                    await Task.Delay(Math.Max(500, _config.RestartDelayMs), ct);
                    Launch();
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    public static string BuildArguments(string startUrl) =>
        $"--start-maximized --no-first-run --no-default-browser-check " +
        "--disable-session-crashed-bubble --disable-features=TranslateUI " +
        $"\"{startUrl}\"";

    private string? ResolveEdgePath()
    {
        var candidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(_config.EdgePath)) candidates.Add(_config.EdgePath);
        candidates.Add(@"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe");
        candidates.Add(@"C:\Program Files\Microsoft\Edge\Application\msedge.exe");
        return candidates.FirstOrDefault(File.Exists);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _watchCts?.Cancel();
        _watchCts?.Dispose();
        try { if (_edge != null && !_edge.HasExited) _edge.Kill(entireProcessTree: true); } catch { }
        _edge?.Dispose();
    }
}
