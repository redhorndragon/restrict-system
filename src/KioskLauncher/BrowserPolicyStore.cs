using KioskGuard.Config;

namespace KioskLauncher;

/// <summary>
/// 加载并热更新服务生成的浏览器白名单文件。文件不可读时返回仅含起始页的降级策略（fail-closed）。
/// </summary>
public sealed class BrowserPolicyStore
{
    private readonly string _path;
    private readonly string _fallbackStartUrl;
    private readonly object _gate = new();
    private BrowserPolicy _current;
    private DateTime _lastWrite;

    public BrowserPolicyStore(string path, string fallbackStartUrl)
    {
        _path = path;
        _fallbackStartUrl = fallbackStartUrl;
        _current = Load();
        Refresh();
    }

    public BrowserPolicy Current
    {
        get
        {
            Refresh();
            lock (_gate) return _current;
        }
    }

    private void Refresh()
    {
        try
        {
            var write = File.Exists(_path) ? File.GetLastWriteTimeUtc(_path) : DateTime.MinValue;
            lock (_gate)
            {
                if (write == _lastWrite) return;
                _lastWrite = write;
                _current = Load();
            }
        }
        catch
        {
            // 读取失败时保持上次有效策略
        }
    }

    private BrowserPolicy Load()
    {
        var policy = BrowserPolicy.Load(_path);
        if (policy != null && !string.IsNullOrWhiteSpace(policy.StartUrl)) return policy;
        return new BrowserPolicy { StartUrl = _fallbackStartUrl };
    }
}
