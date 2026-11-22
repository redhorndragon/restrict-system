namespace KioskGuard.Config;

public sealed class ConfigStore
{
    private readonly object _sync = new();
    private readonly string _path;
    private DateTimeOffset _lastWrite;
    private long _lastLength = -1;

    public ConfigStore(string path)
    {
        _path = path;
        Current = ConfigFile.Load(path);
        CaptureMetadata();
    }

    public AppConfig Current { get; private set; }

    public string Path => _path;

    public event Action<AppConfig>? Reloaded;

    public bool TryReloadIfChanged(out AppConfig? config)
    {
        config = null;
        lock (_sync)
        {
            DateTimeOffset write;
            long length;
            try
            {
                var info = new FileInfo(_path);
                if (!info.Exists) return false;
                info.Refresh();
                write = info.LastWriteTimeUtc;
                length = info.Length;
            }
            catch
            {
                return false;
            }

            if (write == _lastWrite && length == _lastLength) return false;

            try
            {
                var loaded = ConfigFile.Load(_path);
                Current = loaded;
                _lastWrite = write;
                _lastLength = length;
                config = loaded;
                Reloaded?.Invoke(loaded);
                return true;
            }
            catch
            {
                _lastWrite = write;
                _lastLength = length;
                return false;
            }
        }
    }

    private void CaptureMetadata()
    {
        try
        {
            if (File.Exists(_path))
            {
                var info = new FileInfo(_path);
                info.Refresh();
                _lastWrite = info.LastWriteTimeUtc;
                _lastLength = info.Length;
            }
        }
        catch { }
    }
}
