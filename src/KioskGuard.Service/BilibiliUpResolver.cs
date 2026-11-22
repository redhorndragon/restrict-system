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
            foreach (var url in GetAllowedSpaceUrls(upId)) result.Add(url);

            try
            {
                var bvids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var page in new[] { "upload/video", "lists" })
                {
                    var html = await _http.GetStringAsync($"https://space.bilibili.com/{upId}/{page}", ct);
                    foreach (var bvid in ExtractBvids(html)) bvids.Add(bvid);
                }
                foreach (var bvid in bvids)
                {
                    result.Add($"https://www.bilibili.com/video/{bvid}/");
                }
                _logger.LogInformation("UP 主 {UpId} 投稿/合集页解析完成，提取到 {Count} 个视频链接。", upId, bvids.Count);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "UP 主 {UpId} 投稿/合集页爬取失败，仅放行页面本身（降级）。", upId);
            }
        }
        return result.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static IReadOnlyList<string> GetAllowedSpaceUrls(string upId) =>
    [
        $"https://space.bilibili.com/{upId}/upload/video",
        $"https://space.bilibili.com/{upId}/lists",
    ];

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
