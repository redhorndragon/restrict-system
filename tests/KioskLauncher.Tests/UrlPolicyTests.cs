using KioskGuard.Config;
using KioskLauncher;

namespace KioskLauncher.Tests;

public class UrlPolicyTests
{
    private static BrowserPolicy Policy(params string[] allowed) => new()
    {
        StartUrl = "https://portal.example.com/",
        AllowedUrls = allowed.ToList(),
        BilibiliUpIds = ["39737405"],
    };

    [Fact]
    public void StartUrl_Is_Always_Allowed()
    {
        Assert.True(UrlPolicy.IsAllowed("https://portal.example.com/", Policy()));
    }

    [Fact]
    public void Exact_And_Prefix_Allowed()
    {
        var policy = Policy("https://basic.beijing.smartedu.cn/");
        Assert.True(UrlPolicy.IsAllowed("https://basic.beijing.smartedu.cn/", policy));
        Assert.True(UrlPolicy.IsAllowed("https://basic.beijing.smartedu.cn/index", policy));
        Assert.False(UrlPolicy.IsAllowed("https://evil.com/index", policy));
    }

    [Fact]
    public void Non_Whitelisted_Url_Is_Blocked()
    {
        Assert.False(UrlPolicy.IsAllowed("https://www.bing.com/search?q=x", Policy("https://example.com/")));
    }

    [Fact]
    public void Bilibili_Video_From_Allowed_Up_Space_Is_Allowed()
    {
        var policy = Policy();
        const string video = "https://www.bilibili.com/video/BV1FV4y1h7LT/?spm_id_from=333.1387.homepage.video_card.click";
        Assert.True(UrlPolicy.IsAllowed(video, policy, "https://space.bilibili.com/39737405?spm_id_from=333.337.search-card.all.click"));
    }

    [Fact]
    public void Bilibili_Video_From_Other_Up_Is_Blocked()
    {
        var policy = Policy();
        const string video = "https://www.bilibili.com/video/BV1FV4y1h7LT/";
        Assert.False(UrlPolicy.IsAllowed(video, policy, "https://space.bilibili.com/99999999"));
        Assert.False(UrlPolicy.IsAllowed(video, policy));
    }

    [Fact]
    public void Null_Policy_Only_Allows_StartUrl()
    {
        Assert.False(UrlPolicy.IsAllowed("https://example.com/", null));
    }

    [Fact]
    public void Non_Http_Scheme_Is_Blocked()
    {
        Assert.False(UrlPolicy.IsAllowed("javascript:alert(1)", Policy()));
        Assert.False(UrlPolicy.IsAllowed("file:///C:/Windows/", Policy()));
    }
}
