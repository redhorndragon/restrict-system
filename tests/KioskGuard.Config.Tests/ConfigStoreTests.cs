using System.Text.Json;
using KioskGuard.Config;

namespace KioskGuard.Config.Tests;

public class ConfigStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _path;

    public ConfigStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "kiosk-store-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _path = Path.Combine(_tempDir, "config.json");
        Write("https://old.example.com", 10);
    }

    [Fact]
    public void TryReloadIfChanged_DetectsChange_AndRaisesEvent()
    {
        var store = new ConfigStore(_path);
        AppConfig? reloaded = null;
        store.Reloaded += c => reloaded = c;

        Assert.False(store.TryReloadIfChanged(out _));

        Write("https://new.example.com", 20);
        // 确保时间戳变化
        File.SetLastWriteTimeUtc(_path, DateTime.UtcNow.AddSeconds(2));

        Assert.True(store.TryReloadIfChanged(out var config));
        Assert.NotNull(config);
        Assert.Equal("https://new.example.com", config!.GitHubPagesUrl);
        Assert.Equal(20, config.RefreshIntervalMinutes);
        Assert.Same(config, store.Current);
        Assert.Same(config, reloaded);
    }

    [Fact]
    public void TryReloadIfChanged_ParsingFailure_KeepsPreviousConfig()
    {
        var store = new ConfigStore(_path);
        var original = store.Current;

        File.WriteAllText(_path, "not valid json");
        File.SetLastWriteTimeUtc(_path, DateTime.UtcNow.AddSeconds(2));

        Assert.False(store.TryReloadIfChanged(out _));
        Assert.Same(original, store.Current);
    }

    private void Write(string github, int refresh)
    {
        var config = new AppConfig { GitHubPagesUrl = github, StartUrl = github, RefreshIntervalMinutes = refresh };
        File.WriteAllText(_path, JsonSerializer.Serialize(config));
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }
}
