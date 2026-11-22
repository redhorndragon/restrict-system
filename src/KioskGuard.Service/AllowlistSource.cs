using System.Text.Json;
using AngleSharp;
using AngleSharp.Dom;
using KioskGuard.Config;
using KioskGuard.Service.Models;

namespace KioskGuard.Service;

public sealed class AllowlistSource
{
    private readonly ConfigStore _store;
    private readonly ILogger<AllowlistSource> _logger;
    private readonly HttpClient _http;
    private readonly HashSet<string> _current = new(StringComparer.OrdinalIgnoreCase);
    private Allowlist? _cache;

    public AllowlistSource(ConfigStore store, ILogger<AllowlistSource> logger)
    {
        _store = store;
        _logger = logger;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _cache = Allowlist.Load(store.Current.CachePath);
        if (_cache?.Urls != null)
        {
            foreach (var url in _cache.Urls) _current.Add(url);
        }
    }

    public IReadOnlyCollection<string> Current => _current;

    public async Task<IReadOnlyCollection<string>> RefreshAsync(CancellationToken ct)
    {
        var config = _store.Current;
        if (string.IsNullOrWhiteSpace(config.GitHubPagesUrl))
        {
            _logger.LogWarning("GitHubPagesUrl 未配置，保持当前白名单。");
            return _current;
        }

        try
        {
            var html = await _http.GetStringAsync(config.GitHubPagesUrl, ct);
            var parsed = Parse(html, config.GitHubPagesUrl, config.ManifestScriptId, config.AllowlistContainerId);

            if (parsed.Count == 0)
            {
                _logger.LogWarning("未从页面解析出任何链接，保持上一次白名单（fail-closed）。");
                return _current;
            }

            _current.Clear();
            foreach (var url in parsed) _current.Add(url);

            if (!string.IsNullOrWhiteSpace(config.StartUrl)) _current.Add(NormalizeUrl(config.StartUrl, config.GitHubPagesUrl)!);

            if (config.ExtraAllowedUrls != null)
            {
                foreach (var extra in config.ExtraAllowedUrls)
                {
                    var normalized = NormalizeUrl(extra, config.GitHubPagesUrl);
                    if (normalized != null) _current.Add(normalized);
                }
            }

            _cache = new Allowlist
            {
                FetchedAt = DateTimeOffset.UtcNow,
                SourceUrl = config.GitHubPagesUrl,
                Urls = _current.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList(),
            };
            _cache.Save(config.CachePath);

            _logger.LogInformation("白名单已更新，共 {Count} 条。", _current.Count);
            return _current;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "抓取或解析白名单失败，保持上一次白名单（fail-closed）。");
            return _current;
        }
    }

    public static List<string> Parse(string html, string baseUrl, string manifestScriptId, string containerId)
    {
        var context = BrowsingContext.New(Configuration.Default);
        var document = context.OpenAsync(req => req.Content(html)).GetAwaiter().GetResult();

        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var manifest = document.QuerySelector($"script#{CssEscape(manifestScriptId)}");
        if (manifest != null && string.Equals(manifest.GetAttribute("type"), "application/json", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                using var json = JsonDocument.Parse(manifest.TextContent);
                if (json.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var element in json.RootElement.EnumerateArray())
                    {
                        if (element.ValueKind == JsonValueKind.String)
                        {
                            var url = NormalizeUrl(element.GetString() ?? string.Empty, baseUrl);
                            if (url != null) result.Add(url);
                        }
                        else if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("url", out var urlProp))
                        {
                            var url = NormalizeUrl(urlProp.GetString() ?? string.Empty, baseUrl);
                            if (url != null) result.Add(url);
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // manifest 格式错误，回退到链接解析
            }
        }

        if (result.Count == 0)
        {
            var scope = document.QuerySelector($"#{CssEscape(containerId)}") ?? document.Body ?? document.DocumentElement;
            if (scope != null)
            {
                foreach (var anchor in scope.QuerySelectorAll("a[href]"))
                {
                    var url = NormalizeUrl(anchor.GetAttribute("href") ?? string.Empty, baseUrl);
                    if (url != null) result.Add(url);
                }
            }
        }

        return result.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static string? NormalizeUrl(string raw, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        raw = raw.Trim();
        if (raw.StartsWith('#')) return null;
        if (raw.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) ||
            raw.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
            raw.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return null;

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri)) return null;
        if (!Uri.TryCreate(baseUri, raw, out var uri)) return null;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return null;
        return uri.AbsoluteUri;
    }

    private static string CssEscape(string id)
    {
        if (string.IsNullOrEmpty(id)) return string.Empty;
        return string.Concat(id.Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c.ToString() : $"\\{c}"));
    }
}
