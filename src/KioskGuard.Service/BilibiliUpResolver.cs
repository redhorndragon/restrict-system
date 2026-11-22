using System.Text.RegularExpressions;

namespace KioskGuard.Service;

public sealed class BilibiliUpResolver
{
    private static readonly Regex BvidRegex = new(
        @"(?:BV[0-9A-Za-z]{10})",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex BvidInJsonRegex = new(
        @"""bvid""\s*:\s*""(BV[0-9A-Za-z]{10})""",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly ILogger<BilibiliUpResolver> _logger;
    private readonly HttpClient _http;

    public BilibiliUpResolver(ILogger<BilibiliUpResolver> logger)
    {
        _logger = logger;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        _http.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        _http.DefaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.9");
    }

    public async Task<IReadOnlyList<string>> ResolveAsync(IEnumerable<string> upIds, CancellationToken ct)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var upId in upIds.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            ct.ThrowIfCancellationRequested();
            result.Add($"https://space.bilibili.com/{upId}");

            try
            {
                var html = await _http.GetStringAsync($"https://space.bilibili.com/{upId}/video", ct);
                var bvids = ExtractBvids(html);
                foreach (var bvid in bvids)
                {
                    result.Add($"https://www.bilibili.com/video/{bvid}/");
                }
                _logger.LogInformation("UP 主 {UpId} 空间页解析完成，提取到 {Count} 个视频链接。", upId, bvids.Count);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "UP 主 {UpId} 空间页爬取失败，仅放行空间页（降级）。", upId);
            }
        }
        return result.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static IReadOnlyList<string> ExtractBvids(string html)
    {
        if (string.IsNullOrEmpty(html)) return [];
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in BvidInJsonRegex.Matches(html))
        {
            result.Add(match.Groups[1].Value);
        }
        foreach (Match match in BvidRegex.Matches(html))
        {
            result.Add(match.Value);
        }
        return result.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
