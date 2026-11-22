using Microsoft.Win32;
using KioskGuard.Config;

namespace KioskGuard.Service;

public sealed class PolicyApplier
{
    private readonly ConfigStore _store;
    private readonly ILogger<PolicyApplier> _logger;
    private string? _lastSignature;

    public PolicyApplier(ConfigStore store, ILogger<PolicyApplier> logger)
    {
        _store = store;
        _logger = logger;
    }

    public void Invalidate() => _lastSignature = null;

    public void Apply(IReadOnlyCollection<string> urls)
    {
        var config = _store.Current;
        if (string.IsNullOrWhiteSpace(config.TargetUserSid))
        {
            _logger.LogWarning("TargetUserSid 未配置，跳过策略下发。");
            return;
        }

        var signature = string.Join("|",
            new[] { config.StartUrl }.Concat(urls).OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
        if (signature == _lastSignature) return;

        try
        {
            WriteEdgePolicies(urls, config);
            WriteTaskManagerPolicy(config);
            _lastSignature = signature;
            _logger.LogInformation("策略已下发：白名单 {Count} 条。", urls.Count);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "策略暂不可下发（目标用户可能未登录），稍后重试。");
        }
    }

    private void WriteEdgePolicies(IReadOnlyCollection<string> urls, AppConfig config)
    {
        using var edge = OpenUserKey(@"SOFTWARE\Policies\Microsoft\Edge", config);

        ClearValues(edge);

        var ordered = urls.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();

        using var allow = edge.CreateSubKey("URLAllowlist");
        ClearValues(allow);
        for (var i = 0; i < ordered.Count; i++)
        {
            allow.SetValue((i + 1).ToString(), ordered[i], RegistryValueKind.String);
        }

        using var block = edge.CreateSubKey("URLBlocklist");
        ClearValues(block);
        block.SetValue("1", "*", RegistryValueKind.String);

        edge.SetValue("IncognitoModeAvailability", 2, RegistryValueKind.DWord);
        edge.SetValue("DeveloperToolsAvailability", 2, RegistryValueKind.DWord);
        edge.SetValue("DownloadRestrictions", 3, RegistryValueKind.DWord);
        if (!string.IsNullOrWhiteSpace(config.StartUrl))
        {
            edge.SetValue("HomepageLocation", config.StartUrl, RegistryValueKind.String);
            edge.SetValue("HomepageIsNewTabPage", 0, RegistryValueKind.DWord);
            edge.SetValue("NewTabPageLocation", config.StartUrl, RegistryValueKind.String);
        }
    }

    private void WriteTaskManagerPolicy(AppConfig config)
    {
        using var system = OpenUserKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", config);
        system.SetValue("DisableTaskMgr", 1, RegistryValueKind.DWord);
    }

    private static RegistryKey OpenUserKey(string subPath, AppConfig config)
    {
        var path = $@"{config.TargetUserSid}\{subPath}";
        return RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Default)
            .OpenSubKey(path, writable: true)
            ?? throw new InvalidOperationException($"无法打开注册表路径 HKEY_USERS\\{path}（目标用户可能未登录）。");
    }

    private static void ClearValues(RegistryKey key)
    {
        foreach (var name in key.GetValueNames().ToList())
        {
            key.DeleteValue(name, false);
        }
    }
}
