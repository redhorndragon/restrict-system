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
