using KioskGuard.Config;

namespace KioskGuard.Service;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ConfigStore _store;
    private readonly AllowlistSource _allowlistSource;
    private readonly BrowserPolicyWriter _browserPolicyWriter;
    private readonly ProcessGuard _processGuard;
    private readonly SemaphoreSlim _refreshGate;
    private readonly object _wakeLock = new();
    private TaskCompletionSource<bool>? _refreshWake;

    public Worker(ILogger<Worker> logger, ConfigStore store, AllowlistSource allowlistSource,
        BrowserPolicyWriter browserPolicyWriter, ProcessGuard processGuard, SemaphoreSlim refreshGate)
    {
        _logger = logger;
        _store = store;
        _allowlistSource = allowlistSource;
        _browserPolicyWriter = browserPolicyWriter;
        _processGuard = processGuard;
        _refreshGate = refreshGate;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("KioskGuard 服务启动。");
        await RefreshCoreAsync(cancellationToken);
        await _processGuard.StartAsync(cancellationToken);
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var configTask = ConfigWatchLoopAsync(stoppingToken);
        var refreshTask = RefreshLoopAsync(stoppingToken);
        var retryTask = RetryLoopAsync(stoppingToken);
        try
        {
            await Task.WhenAll(configTask, refreshTask, retryTask);
        }
        catch (OperationCanceledException) { }
    }

    private async Task ConfigWatchLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                if (!_store.TryReloadIfChanged(out var config) || config == null) continue;

                _logger.LogInformation("检测到配置变更，立即刷新白名单与策略。");
                _browserPolicyWriter.Invalidate();
                SignalRefreshCycle();
                await RefreshCoreAsync(ct);
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task RefreshLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var period = TimeSpan.FromMinutes(Math.Max(1, _store.Current.RefreshIntervalMinutes));
                var wake = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                lock (_wakeLock) _refreshWake = wake;

                var delay = Task.Delay(period, ct);
                var completed = await Task.WhenAny(delay, wake.Task);
                if (completed == wake.Task) continue;

                await RefreshCoreAsync(ct);
            }
        }
        catch (OperationCanceledException) { }
    }

    private void SignalRefreshCycle()
    {
        lock (_wakeLock) _refreshWake?.TrySetResult(true);
    }

    private async Task RefreshCoreAsync(CancellationToken ct)
    {
        await _refreshGate.WaitAsync(ct);
        try
        {
            var urls = await _allowlistSource.RefreshAsync(ct);
            _browserPolicyWriter.Write(urls, _allowlistSource.BilibiliUpIds);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        finally
        {
            _refreshGate.Release();
        }
    }

    private async Task RetryLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                _browserPolicyWriter.Write(_allowlistSource.Current, _allowlistSource.BilibiliUpIds);
            }
        }
        catch (OperationCanceledException) { }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("KioskGuard 服务停止。");
        _processGuard.Dispose();
        await base.StopAsync(cancellationToken);
    }
}
