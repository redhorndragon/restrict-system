using KioskGuard.Config;

namespace KioskAdmin;

public partial class MainForm : Form
{
    private const string DefaultConfigPath = @"C:\ProgramData\KioskGuard\config.json";
    private const string DefaultLauncherPath = @"C:\Program Files\KioskGuard\launcher.json";
    private const string DefaultErrorHint = "";

    private readonly string _adminName;
    private readonly string _configPath;
    private readonly string _launcherPath;
    private readonly List<string> _extraUrls = [];
    private bool _loading = true;

    public MainForm(string adminName)
    {
        _adminName = adminName;
        _configPath = Environment.GetEnvironmentVariable("KIOSKGUARD_CONFIG") ?? DefaultConfigPath;
        _launcherPath = Environment.GetEnvironmentVariable("KIOSKGUARD_LAUNCHER") ?? DefaultLauncherPath;
        InitializeComponent();
        subtitleLabel.Text = $"管理对象：study    ·    当前管理员：{_adminName}";

        reloadButton.Click += ReloadButton_Click;
        saveButton.Click += SaveButton_Click;
        addUrlButton.Click += AddUrlButton_Click;
        removeUrlButton.Click += RemoveUrlButton_Click;
        newUrlTextBox.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) AddUrlButton_Click(null, EventArgs.Empty); };
        githubTextBox.TextChanged += (_, _) => ClearFieldError(githubError, _githubBorder);
        startTextBox.TextChanged += (_, _) => ClearFieldError(startError, _startBorder);
        refreshNumeric.ValueChanged += (_, _) => ClearFieldError(refreshError, refreshNumeric);
        newUrlTextBox.TextChanged += (_, _) => urlError.Text = DefaultErrorHint;

        LoadConfig();
    }

    private void LoadConfig()
    {
        _loading = true;
        ClearAllErrors();
        try
        {
            DebugLog($"LoadConfig: path={_configPath}, exists={File.Exists(_configPath)}");
            if (!File.Exists(_configPath))
            {
                SetStatus(false, "未能读取配置文件，请确认 KioskGuard 已正确安装。");
                inlineStatus.Text = "配置文件不存在";
                saveButton.Enabled = false;
                return;
            }

            var config = ConfigFile.Load(_configPath);
            DebugLog($"Loaded: URL=[{config.GitHubPagesUrl}] START=[{config.StartUrl}] EXTRA=[{config.ExtraAllowedUrls.Count}]");
            githubTextBox.Text = config.GitHubPagesUrl;
            startTextBox.Text = config.StartUrl;
            DebugLog($"Assigned: tbURL=[{githubTextBox.Text}] tbSTART=[{startTextBox.Text}]");
            refreshNumeric.Value = Math.Max(refreshNumeric.Minimum,
                Math.Min(refreshNumeric.Maximum, config.RefreshIntervalMinutes));
            _extraUrls.Clear();
            if (config.ExtraAllowedUrls != null)
                _extraUrls.AddRange(config.ExtraAllowedUrls.Where(u => !string.IsNullOrWhiteSpace(u)));
            RefreshUrlList();
            saveButton.Enabled = true;
            SetStatus(true, "配置已加载。");
            inlineStatus.Text = "可修改白名单源配置";
        }
        catch (Exception ex)
        {
            DebugLog($"ERROR: {ex}");
            SetStatus(false, $"读取配置失败：{ex.Message}");
            inlineStatus.Text = "读取失败";
            saveButton.Enabled = false;
        }
        finally
        {
            _loading = false;
        }
    }

    private void RefreshUrlList()
    {
        urlListBox.BeginUpdate();
        urlListBox.Items.Clear();
        foreach (var url in _extraUrls) urlListBox.Items.Add(url);
        urlListBox.EndUpdate();
    }

    private void AddUrlButton_Click(object? sender, EventArgs e)
    {
        var url = newUrlTextBox.Text.Trim();
        if (string.IsNullOrEmpty(url)) return;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            urlError.Text = "请输入有效的 http/https 地址。";
            return;
        }
        if (_extraUrls.Any(u => string.Equals(u, url, StringComparison.OrdinalIgnoreCase)))
        {
            urlError.Text = "该地址已在列表中。";
            return;
        }
        _extraUrls.Add(url);
        RefreshUrlList();
        newUrlTextBox.Clear();
        urlError.Text = DefaultErrorHint;
    }

    private void RemoveUrlButton_Click(object? sender, EventArgs e)
    {
        if (urlListBox.SelectedItem is not string selected) return;
        _extraUrls.RemoveAll(u => string.Equals(u, selected, StringComparison.OrdinalIgnoreCase));
        RefreshUrlList();
        urlError.Text = DefaultErrorHint;
    }

    private static void DebugLog(string message)
    {
        try
        {
            var dir = Path.Combine(Path.GetTempPath(), "KioskGuard");
            Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "admin-debug.log"),
                $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
        }
        catch { }
    }

    private void ReloadButton_Click(object? sender, EventArgs e) => LoadConfig();

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        if (_loading) return;
        ClearAllErrors();

        var github = githubTextBox.Text.Trim();
        var start = startTextBox.Text.Trim();
        var refresh = (int)refreshNumeric.Value;

        var validation = ConfigFile.Validate(github, start, refresh);
        if (!validation.IsValid)
        {
            if (validation.Errors.TryGetValue(nameof(AppConfig.GitHubPagesUrl), out var gErr))
                ShowFieldError(githubError, _githubBorder, gErr);
            if (validation.Errors.TryGetValue(nameof(AppConfig.StartUrl), out var sErr))
                ShowFieldError(startError, _startBorder, sErr);
            if (validation.Errors.TryGetValue(nameof(AppConfig.RefreshIntervalMinutes), out var rErr))
                ShowFieldError(refreshError, refreshNumeric, rErr);

            var firstError = validation.Errors.Values.FirstOrDefault();
            if (!string.IsNullOrEmpty(firstError)) FocusFirstInvalid(validation);
            SetStatus(false, firstError ?? "配置校验失败。");
            inlineStatus.Text = "请检查输入";
            return;
        }

        saveButton.Enabled = false;
        inlineStatus.Text = "保存中…";
        try
        {
            ConfigFile.SaveWhitelistSource(_configPath, github, start, refresh);
            ConfigFile.SaveExtraAllowedUrls(_configPath, _extraUrls);
            ConfigFile.SaveLauncherStartUrl(_launcherPath, start);
            SetStatus(true, "保存成功，服务将立即刷新白名单与策略。");
            inlineStatus.Text = "已保存并生效";
        }
        catch (Exception ex)
        {
            SetStatus(false, $"保存失败：{ex.Message}");
            inlineStatus.Text = "写入失败，原配置未被修改";
        }
        finally
        {
            saveButton.Enabled = true;
        }
    }

    private void FocusFirstInvalid(ValidationResult result)
    {
        if (result.Errors.ContainsKey(nameof(AppConfig.GitHubPagesUrl))) githubTextBox.Focus();
        else if (result.Errors.ContainsKey(nameof(AppConfig.StartUrl))) startTextBox.Focus();
        else if (result.Errors.ContainsKey(nameof(AppConfig.RefreshIntervalMinutes))) refreshNumeric.Focus();
    }

    private void ClearAllErrors()
    {
        githubError.Text = DefaultErrorHint;
        startError.Text = DefaultErrorHint;
        refreshError.Text = DefaultErrorHint;
        urlError.Text = DefaultErrorHint;
        _githubBorder.BorderColor = Color.FromArgb(0xD1, 0xD5, 0xDB);
        _startBorder.BorderColor = Color.FromArgb(0xD1, 0xD5, 0xDB);
        refreshNumeric.BackColor = Color.White;
    }

    private static void ClearFieldError(Label error, RoundPanel border)
    {
        error.Text = DefaultErrorHint;
        border.BorderColor = Color.FromArgb(0xD1, 0xD5, 0xDB);
    }

    private static void ClearFieldError(Label error, NumericUpDown input)
    {
        error.Text = DefaultErrorHint;
        input.BackColor = Color.White;
    }

    private static void ShowFieldError(Label error, RoundPanel border, string message)
    {
        error.Text = message;
        border.BorderColor = Color.FromArgb(0xDC, 0x26, 0x26);
    }

    private static void ShowFieldError(Label error, NumericUpDown input, string message)
    {
        error.Text = message;
        input.BackColor = Color.FromArgb(0xFD, 0xEF, 0xEF);
    }

    private void SetStatus(bool success, string message)
    {
        statusText.Text = message;
        statusText.ForeColor = success
            ? Color.FromArgb(0x16, 0xA3, 0x4A)
            : Color.FromArgb(0xDC, 0x26, 0x26);
        statusDot.BackColor = success
            ? Color.FromArgb(0x16, 0xA3, 0x4A)
            : Color.FromArgb(0xDC, 0x26, 0x26);
        statusDot.Refresh();
    }
}
