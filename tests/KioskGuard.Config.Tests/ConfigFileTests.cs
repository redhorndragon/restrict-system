using System.Text.Json.Nodes;
using KioskGuard.Config;

namespace KioskGuard.Config.Tests;

public class ConfigFileTests : IDisposable
{
    private readonly string _tempDir;

    public ConfigFileTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "kiosk-config-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [Theory]
    [InlineData("https://example.com", "https://example.com", 10, true)]
    [InlineData("http://example.com/a", "http://example.com/b", 1, true)]
    [InlineData("ftp://example.com", "https://example.com", 10, false)]
    [InlineData("https://example.com", "not-a-url", 10, false)]
    [InlineData("https://example.com", "https://example.com", 0, false)]
    [InlineData("https://example.com", "https://example.com", 1441, false)]
    public void Validate_EnforcesHttpAndRange(string github, string start, int refresh, bool expected)
    {
        var result = ConfigFile.Validate(github, start, refresh);
        Assert.Equal(expected, result.IsValid);
    }

    [Fact]
    public void SaveWhitelistSource_UpdatesOnlyManagedKeys()
    {
        var path = Path.Combine(_tempDir, "config.json");
        var root = new JsonObject
        {
            ["GitHubPagesUrl"] = "https://old.example.com",
            ["StartUrl"] = "https://old.example.com",
            ["RefreshIntervalMinutes"] = 5,
            ["TargetUserSid"] = "S-1-5-21-123",
            ["TargetUserName"] = "study",
            ["AllowedProcessNames"] = new JsonArray("msedge.exe"),
        };
        File.WriteAllText(path, root.ToJsonString());

        ConfigFile.SaveWhitelistSource(path, "https://new.example.com", "https://start.example.com", 30);

        var updated = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        Assert.Equal("https://new.example.com", updated["GitHubPagesUrl"]!.GetValue<string>());
        Assert.Equal("https://start.example.com", updated["StartUrl"]!.GetValue<string>());
        Assert.Equal(30, updated["RefreshIntervalMinutes"]!.GetValue<int>());
        Assert.Equal("S-1-5-21-123", updated["TargetUserSid"]!.GetValue<string>());
        Assert.Equal("study", updated["TargetUserName"]!.GetValue<string>());
        Assert.Single(updated["AllowedProcessNames"]!.AsArray());
    }

    [Fact]
    public void SaveWhitelistSource_RejectsInvalidInput_WithoutTouchingFile()
    {
        var path = Path.Combine(_tempDir, "config.json");
        File.WriteAllText(path, "{\"GitHubPagesUrl\":\"original\"}");

        Assert.Throws<ArgumentException>(() =>
            ConfigFile.SaveWhitelistSource(path, "ftp://bad", "https://ok", 10));

        Assert.Contains("original", File.ReadAllText(path));
    }

    [Fact]
    public void SaveLauncherStartUrl_UpdatesStartUrl()
    {
        var path = Path.Combine(_tempDir, "launcher.json");
        File.WriteAllText(path, "{\"StartUrl\":\"https://old\",\"RestartDelayMs\":1500}");

        ConfigFile.SaveLauncherStartUrl(path, "https://new");

        var updated = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        Assert.Equal("https://new", updated["StartUrl"]!.GetValue<string>());
        Assert.Equal(1500, updated["RestartDelayMs"]!.GetValue<int>());
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }
}
