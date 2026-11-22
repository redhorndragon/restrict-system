using KioskGuard.Config;

namespace KioskLauncher;

/// <summary>
/// 纯逻辑的 URL 白名单判定，便于单元测试。
/// </summary>
public static class UrlPolicy
{
    private static readonly string[] BiliVideoHosts = ["www.bilibili.com", "bilibili.com", "m.bilibili.com"];
    private static readonly string[] BiliSpaceHosts = ["space.bilibili.com"];

    public static bool IsAllowed(string url, BrowserPolicy? policy, string? referer = null)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var target)) return false;

        if (target.Scheme == Uri.UriSchemeHttps || target.Scheme == Uri.UriSchemeHttp)
        {
            // 起始页永远允许，保证浏览器至少能打开管理页面
            if (Matches(target, policy?.StartUrl)) return true;

            if (policy == null) return false;

            foreach (var allowed in policy.AllowedUrls)
            {
                if (Matches(target, allowed)) return true;
            }

            // B 站来源链：从白名单 UP 主空间页点击跳转的视频页放行
            if (IsBilibiliVideo(target) && IsFromAllowedBilibiliSpace(referer, policy))
            {
                return true;
            }
        }

        return false;
    }

    public static bool Matches(Uri target, string? allowedRaw)
    {
        if (string.IsNullOrWhiteSpace(allowedRaw)) return false;
        if (!Uri.TryCreate(allowedRaw, UriKind.Absolute, out var allowed)) return false;
        if (allowed.Scheme != target.Scheme) return false;
        if (!string.Equals(allowed.Host, target.Host, StringComparison.OrdinalIgnoreCase)) return false;
        if (allowed.Port != target.Port) return false;

        var targetPath = target.AbsolutePath.TrimEnd('/');
        var allowedPath = allowed.AbsolutePath.TrimEnd('/');
        if (allowedPath.Length == 0) return true;

        return string.Equals(targetPath, allowedPath, StringComparison.OrdinalIgnoreCase) ||
               targetPath.StartsWith(allowedPath + "/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBilibiliVideo(Uri uri)
    {
        if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) return false;
        if (!BiliVideoHosts.Any(h => string.Equals(uri.Host, h, StringComparison.OrdinalIgnoreCase))) return false;
        var path = uri.AbsolutePath;
        return path.StartsWith("/video/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsFromAllowedBilibiliSpace(string? referer, BrowserPolicy policy)
    {
        if (string.IsNullOrWhiteSpace(referer)) return false;
        if (!Uri.TryCreate(referer, UriKind.Absolute, out var source)) return false;
        if (source.Scheme != Uri.UriSchemeHttps && source.Scheme != Uri.UriSchemeHttp) return false;
        if (!BiliSpaceHosts.Any(h => string.Equals(source.Host, h, StringComparison.OrdinalIgnoreCase))) return false;

        var segments = source.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0) return false;
        var upId = segments[0];
        return policy.BilibiliUpIds.Any(id => string.Equals(id, upId, StringComparison.OrdinalIgnoreCase));
    }
}
