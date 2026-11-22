using KioskGuard.Config;

namespace KioskGuard.Config.Tests;

public class BrowserPolicyTests
{
    [Fact]
    public void Save_And_Load_Roundtrip()
    {
        var dir = Path.Combine(Path.GetTempPath(), "kiosk-policy-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(dir, "browser-policy.json");
        Directory.CreateDirectory(dir);
        try
        {
            var policy = new BrowserPolicy
            {
                StartUrl = "https://portal.example.com/",
                AllowedUrls = ["https://example.com/"],
                BilibiliUpIds = ["39737405"],
            };
            policy.Save(path);
            var loaded = BrowserPolicy.Load(path);
            Assert.NotNull(loaded);
            Assert.Equal(policy.StartUrl, loaded!.StartUrl);
            Assert.Equal(policy.AllowedUrls, loaded.AllowedUrls);
            Assert.Equal(policy.BilibiliUpIds, loaded.BilibiliUpIds);
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { }
        }
    }

    [Fact]
    public void Load_Returns_Null_When_Missing()
    {
        Assert.Null(BrowserPolicy.Load(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))));
    }
}
