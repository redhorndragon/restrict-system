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
        githubTextBox.TextChanged += (_, _) => ClearFieldError(githubError, _githubBorder);
        startTextBox.TextChanged += (_, _) => ClearFieldError(startError, _startBorder);
        refreshNumeric.ValueChanged += (_, _) => ClearFieldError(refreshError, refreshNumeric);

        LoadConfig();
    }

    private void LoadConfig()
    {
        _loading = true;
        ClearAllErrors();
        try
        {
            if (!File.Exists(_configPath))
            {
                SetStatus(false, "未能读取配置文件，请确认 KioskGuard 已正确安装。");
                inlineStatus.Text = "配置文件不存在";
                saveButton.Enabled = false;
                return;
            }

            var config = ConfigFile.Load(_configPath);
            githubTextBox.Text = config.GitHubPagesUrl;
            startTextBox.Text = config.StartUrl;
            refreshNumeric.Value = Math.Max(refreshNumeric.Minimum,
                Math.Min(refreshNumeric.Maximum, config.RefreshIntervalMinutes));
            saveButton.Enabled = true;
            SetStatus(true, "配置已加载。");
            inlineStatus.Text = "可修改白名单源配置";
        }
        catch (Exception ex)
        {
            SetStatus(false, $"读取配置失败：{ex.Message}");
            inlineStatus.Text = "读取失败";
            saveButton.Enabled = false;
        }
        finally
        {
            _loading = false;
        }
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
