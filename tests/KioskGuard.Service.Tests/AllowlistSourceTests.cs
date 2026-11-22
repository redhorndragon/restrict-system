using KioskGuard.Service;

namespace KioskGuard.Service.Tests;

public class AllowlistSourceTests
{
    private const string BaseUrl = "https://example.github.io/repo/allowlist.html";

    [Fact]
    public void Parse_Manifest_Json_Array_Of_Strings()
    {
        var html = """
            <html><body>
            <script type="application/json" id="kiosk-allowlist">
            ["https://a.example.com", "/relative", "https://b.example.com/path"]
            </script>
            </body></html>
            """;

        var urls = AllowlistSource.Parse(html, BaseUrl, "kiosk-allowlist", "kiosk-allowlist");

        Assert.Contains("https://a.example.com/", urls);
        Assert.Contains("https://example.github.io/relative", urls);
        Assert.Contains("https://b.example.com/path", urls);
        Assert.Equal(3, urls.Count);
    }

    [Fact]
    public void Parse_Manifest_Json_Array_Of_Objects()
    {
        var html = """
            <script type="application/json" id="kiosk-allowlist">
            [{"url":"https://a.example.com"},{"url":"https://b.example.com"}]
            </script>
            """;

        var urls = AllowlistSource.Parse(html, BaseUrl, "kiosk-allowlist", "kiosk-allowlist");

        Assert.Equal(2, urls.Count);
    }

    [Fact]
    public void Parse_Falls_Back_To_Anchors_In_Container()
    {
        var html = """
            <html><body>
            <a href="https://blocked.example.com">外部</a>
            <div id="kiosk-allowlist">
              <a href="https://allowed.example.com">允许</a>
              <a href="/docs">相对</a>
              <a href="javascript:alert(1)">脚本</a>
            </div>
            </body></html>
            """;

        var urls = AllowlistSource.Parse(html, BaseUrl, "kiosk-allowlist", "kiosk-allowlist");

        Assert.DoesNotContain("https://blocked.example.com/", urls);
        Assert.Contains("https://allowed.example.com/", urls);
        Assert.Contains("https://example.github.io/docs", urls);
        Assert.DoesNotContain(urls, u => u.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Parse_Deduplicates_And_Sorts()
    {
        var html = """
            <div id="kiosk-allowlist">
            <a href="https://b.example.com">B</a>
            <a href="https://a.example.com">A</a>
            <a href="https://a.example.com">A2</a>
            </div>
            """;

        var urls = AllowlistSource.Parse(html, BaseUrl, "kiosk-allowlist", "kiosk-allowlist");

        Assert.Equal(2, urls.Count);
        Assert.Equal("https://a.example.com/", urls[0]);
        Assert.Equal("https://b.example.com/", urls[1]);
    }

    [Fact]
    public void ParseRules_ExtractsBilibiliUpRuleAlongsideUrls()
    {
        var html = """
            <script type="application/json" id="kiosk-allowlist">
            ["https://example.com/", "bilibili:up=23630128", "bilibili:up=123"]
            </script>
            """;

        var rules = AllowlistSource.ParseRules(html, BaseUrl, "kiosk-allowlist", "kiosk-allowlist");

        Assert.Single(rules.Urls);
        Assert.Equal("https://example.com/", rules.Urls[0]);
        Assert.Equal(2, rules.BilibiliUpIds.Count);
        Assert.Contains("23630128", rules.BilibiliUpIds);
        Assert.Contains("123", rules.BilibiliUpIds);
    }

    [Fact]
    public void Parse_IgnoresBilibiliUpRuleInPlainUrlList()
    {
        var html = """
            <script type="application/json" id="kiosk-allowlist">
            ["https://example.com/", "bilibili:up=23630128"]
            </script>
            """;

        var urls = AllowlistSource.Parse(html, BaseUrl, "kiosk-allowlist", "kiosk-allowlist");

        Assert.Single(urls);
    }

    [Theory]
    [InlineData("bilibili:up=23630128", true, "23630128")]
    [InlineData("BILIBILI:UP=123", true, "123")]
    [InlineData("bilibili:up=abc", false, null)]
    [InlineData("bilibili:up=", false, null)]
    [InlineData("https://example.com", false, null)]
    public void TryParseBilibiliUpRule_Works(string raw, bool expected, string? expectedId)
    {
        var result = AllowlistSource.TryParseBilibiliUpRule(raw, out var id);
        Assert.Equal(expected, result);
        Assert.Equal(expectedId, id);
    }

    [Fact]
    public void ExtractBvids_FindsBvidInJsonAndUrls()
    {
        var html = """
            <script>window.__INITIAL_STATE__ = {"video":{"bvid":"BV1xx411c7mD"}};</script>
            <a href="https://www.bilibili.com/video/BV1GJ411x7h7/">视频</a>
            <a href="/video/BV1GJ411x7h7/">重复</a>
            """;

        var bvids = BilibiliUpResolver.ExtractBvids(html);

        Assert.Equal(2, bvids.Count);
        Assert.Contains("BV1xx411c7mD", bvids);
        Assert.Contains("BV1GJ411x7h7", bvids);
    }

    [Fact]
    public void ExtractBvids_EmptyHtml_ReturnsEmpty()
    {
        Assert.Empty(BilibiliUpResolver.ExtractBvids(""));
    }

    [Theory]
    [InlineData("https://a.example.com/path", "https://a.example.com/path")]
    [InlineData("/docs", "https://example.github.io/docs")]
    [InlineData("mailto:a@b.com", null)]
    [InlineData("javascript:alert(1)", null)]
    [InlineData("#section", null)]
    public void NormalizeUrl_Works(string raw, string? expected)
    {
        var actual = AllowlistSource.NormalizeUrl(raw, BaseUrl);
        Assert.Equal(expected, actual);
    }
}
