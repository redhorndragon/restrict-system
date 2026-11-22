using KioskGuard.Config;

namespace KioskGuard.Service;

/// <summary>
/// 将白名单与 B 站规则写入自研浏览器读取的策略文件。不再下发任何浏览器注册表策略。
/// </summary>
public sealed class BrowserPolicyWriter
{
    private readonly ConfigStore _store;
    private readonly ILogger<BrowserPolicyWriter> _logger;
    private string? _lastSignature;

    public BrowserPolicyWriter(ConfigStore store, ILogger<BrowserPolicyWriter> logger)
    {
        _store = store;
        _logger = logger;
    }

    public void Invalidate() => _lastSignature = null;

    public void Write(IReadOnlyCollection<string> urls, IReadOnlyCollection<string> upIds)
    {
        var config = _store.Current;
        var signature = string.Join("|",
            new[] { config.StartUrl }.Concat(urls).Concat(upIds).OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
        if (signature == _lastSignature && File.Exists(config.BrowserPolicyPath)) return;

        try
        {
            var policy = new BrowserPolicy
            {
                FetchedAt = DateTimeOffset.UtcNow,
                SourceUrl = config.GitHubPagesUrl,
                StartUrl = config.StartUrl,
                AllowedUrls = urls.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList(),
                BilibiliUpIds = upIds.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList(),
            };
            policy.Save(config.BrowserPolicyPath);
            _lastSignature = signature;
            _logger.LogInformation("浏览器策略已写入：白名单 {Count} 条，B站UP主 {UpCount} 个。",
                urls.Count, upIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "浏览器策略写入失败，稍后重试。");
        }
    }
}
