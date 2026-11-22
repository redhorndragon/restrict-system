using KioskGuard.Config;
using KioskGuard.Service;

namespace KioskGuard.Service.Tests;

public class BrowserPolicyWriterTests
{
    [Fact]
    public void Write_Creates_Policy_With_Urls_And_UpIds()
    {
        var dir = Path.Combine(Path.GetTempPath(), "kiosk-test-" + Guid.NewGuid().ToString("N"));
        var configPath = Path.Combine(dir, "config.json");
        var policyPath = Path.Combine(dir, "browser-policy.json");
        Directory.CreateDirectory(dir);
        try
        {
            var config = new AppConfig
            {
                StartUrl = "https://portal.example.com/",
                GitHubPagesUrl = "https://pages.example.com/allowlist.html",
                BrowserPolicyPath = policyPath,
            };
            File.WriteAllText(configPath, System.Text.Json.JsonSerializer.Serialize(config,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            var store = new ConfigStore(configPath);
            var writer = new BrowserPolicyWriter(store, Microsoft.Extensions.Logging.Abstractions.NullLogger<BrowserPolicyWriter>.Instance);

            writer.Write(["https://example.com/", "https://portal.example.com/"], ["39737405"]);

            var policy = BrowserPolicy.Load(policyPath);
            Assert.NotNull(policy);
            Assert.Equal("https://portal.example.com/", policy!.StartUrl);
            Assert.Contains("https://example.com/", policy.AllowedUrls);
            Assert.Contains("39737405", policy.BilibiliUpIds);
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { }
        }
    }
}
