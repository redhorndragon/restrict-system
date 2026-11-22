#nullable enable
namespace KioskAdmin;

partial class MainForm
{
    private System.ComponentModel.IContainer? components = null;

    private Panel headerPanel = null!;
    private RoundPanel logoBadge = null!;
    private Label logoLetter = null!;
    private Label titleLabel = null!;
    private Label subtitleLabel = null!;
    private RoundPanel adminBadge = null!;
    private Label adminBadgeText = null!;

    private Panel contentPanel = null!;
    private RoundPanel configCard = null!;
    private Label configCardTitle = null!;
    private Label githubLabel = null!;
    private TextBox githubTextBox = null!;
    private Label githubHint = null!;
    private Label githubError = null!;
    private Label startLabel = null!;
    private TextBox startTextBox = null!;
    private Label startHint = null!;
    private Label startError = null!;
    private Label refreshLabel = null!;
    private NumericUpDown refreshNumeric = null!;
    private Label refreshHint = null!;
    private Label refreshError = null!;

    private RoundPanel actionCard = null!;
    private Button reloadButton = null!;
    private Button saveButton = null!;
    private Label inlineStatus = null!;

    private Panel statusBar = null!;
    private Label statusDot = null!;
    private Label statusText = null!;
    private Label effectHint = null!;

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        SuspendLayout();

            var primary = Color.FromArgb(0x25, 0x63, 0xEB);
        var primaryDark = Color.FromArgb(0x1D, 0x4E, 0xD8);
        var bg = Color.FromArgb(0xF5, 0xF7, 0xFA);
        var textDark = Color.FromArgb(0x1F, 0x29, 0x37);
        var textMuted = Color.FromArgb(0x6B, 0x72, 0x80);

        Text = "Kiosk 访问限制管理";
        ClientSize = new Size(760, 500);
        MinimumSize = new Size(700, 460);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = bg;
        Font = UiFont(12f, FontStyle.Regular);
        ForeColor = textDark;

        // 顶部标题区
        headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 96,
            BackColor = Color.White,
        };
        headerPanel.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(0xE5, 0xE7, 0xEB));
            e.Graphics.DrawLine(pen, 0, headerPanel.Height - 1, headerPanel.Width, headerPanel.Height - 1);
        };

        logoBadge = new RoundPanel { Size = new Size(44, 44), Location = new Point(24, 26), BackColor = primary, CornerRadius = 10, DrawBorder = false };
        logoLetter = new Label { Text = "K", Dock = DockStyle.Fill, Font = UiFont(22f, FontStyle.Bold), ForeColor = Color.White, TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent };
        logoBadge.Controls.Add(logoLetter);

        titleLabel = new Label { Text = "Kiosk 访问限制管理", Location = new Point(82, 22), AutoSize = true, Font = UiFont(24f, FontStyle.Bold), ForeColor = textDark };
        subtitleLabel = new Label { Text = "管理对象：study    ·    当前管理员：gbr", Location = new Point(84, 58), AutoSize = true, Font = UiFont(13f, FontStyle.Regular), ForeColor = textMuted };

        adminBadge = new RoundPanel { Size = new Size(132, 34), Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(600, 31), BackColor = Color.FromArgb(0xEF, 0xF6, 0xFF), CornerRadius = 17, BorderColor = Color.FromArgb(0xDB, 0xEA, 0xFF) };
        adminBadgeText = new Label { Text = "已验证管理员", Dock = DockStyle.Fill, Font = UiFont(12f, FontStyle.Bold), ForeColor = primaryDark, TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent };
        adminBadge.Controls.Add(adminBadgeText);

        headerPanel.Controls.Add(logoBadge);
        headerPanel.Controls.Add(titleLabel);
        headerPanel.Controls.Add(subtitleLabel);
        headerPanel.Controls.Add(adminBadge);

        // 内容区
        contentPanel = new Panel { Dock = DockStyle.Fill, BackColor = bg, Padding = new Padding(20, 18, 20, 10), AutoScroll = true };

        configCard = new RoundPanel { Location = new Point(20, 18), Size = new Size(700, 244), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, BackColor = Color.White, CornerRadius = 12 };
        configCardTitle = new Label { Text = "白名单源配置", Location = new Point(22, 16), AutoSize = true, Font = UiFont(14f, FontStyle.Bold), ForeColor = textDark };

        githubLabel = FieldLabel("GitHub Pages 地址", 22, 56);
        var githubBorder = new RoundPanel { Location = new Point(22, 78), Size = new Size(440, 34), CornerRadius = 8, BackColor = Color.White, BorderColor = Color.FromArgb(0xD1, 0xD5, 0xDB) };
        githubTextBox = FieldInput(22, 78, githubBorder);
        githubHint = FieldHint("用于解析允许链接的公开静态页面", 480, 84, 210, textMuted);
        githubError = FieldError(22, 112, "");

        startLabel = FieldLabel("Edge 起始页", 22, 132);
        var startBorder = new RoundPanel { Location = new Point(22, 154), Size = new Size(440, 34), CornerRadius = 8, BackColor = Color.White, BorderColor = Color.FromArgb(0xD1, 0xD5, 0xDB) };
        startTextBox = FieldInput(22, 154, startBorder);
        startHint = FieldHint("Kiosk 模式下 Edge 打开的首个地址", 480, 160, 210, textMuted);
        startError = FieldError(22, 188, "");

        refreshLabel = FieldLabel("刷新周期（分钟）", 22, 208);
        refreshNumeric = new NumericUpDown
        {
            Location = new Point(22, 230),
            Size = new Size(160, 32),
            Minimum = 1,
            Maximum = 1440,
            Value = 10,
            Font = UiFont(12f, FontStyle.Regular),
            BackColor = Color.White,
            ForeColor = textDark,
            BorderStyle = BorderStyle.FixedSingle,
        };
        refreshHint = FieldHint("1 - 1440 分钟，修改后立即生效", 200, 236, 300, textMuted);
        refreshError = FieldError(22, 266, "");
        configCard.Height = 306;

        configCard.Controls.Add(configCardTitle);
        configCard.Controls.Add(githubLabel);
        configCard.Controls.Add(githubBorder);
        configCard.Controls.Add(githubTextBox);
        configCard.Controls.Add(githubHint);
        configCard.Controls.Add(githubError);
        configCard.Controls.Add(startLabel);
        configCard.Controls.Add(startBorder);
        configCard.Controls.Add(startTextBox);
        configCard.Controls.Add(startHint);
        configCard.Controls.Add(startError);
        configCard.Controls.Add(refreshLabel);
        configCard.Controls.Add(refreshNumeric);
        configCard.Controls.Add(refreshHint);
        configCard.Controls.Add(refreshError);

        contentPanel.Controls.Add(configCard);

        // 操作区
        actionCard = new RoundPanel { Location = new Point(20, 340), Size = new Size(700, 74), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, BackColor = Color.White, CornerRadius = 12 };
        reloadButton = new Button
        {
            Text = "从文件重新加载",
            Location = new Point(22, 20),
            Size = new Size(140, 36),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = textDark,
            Font = UiFont(12f, FontStyle.Bold),
            Cursor = Cursors.Hand,
        };
        reloadButton.FlatAppearance.BorderColor = Color.FromArgb(0xD1, 0xD5, 0xDB);
        reloadButton.FlatAppearance.BorderSize = 1;

        saveButton = new Button
        {
            Text = "保存并立即生效",
            Location = new Point(538, 20),
            Size = new Size(140, 36),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = primary,
            ForeColor = Color.White,
            Font = UiFont(12f, FontStyle.Bold),
            Cursor = Cursors.Hand,
        };
        saveButton.FlatAppearance.BorderSize = 0;
        saveButton.MouseEnter += (_, _) => saveButton.BackColor = primaryDark;
        saveButton.MouseLeave += (_, _) => { if (saveButton.Enabled) saveButton.BackColor = primary; };

        inlineStatus = new Label { Text = "", Location = new Point(180, 28), Size = new Size(340, 24), Font = UiFont(11f, FontStyle.Regular), ForeColor = textMuted, TextAlign = ContentAlignment.MiddleLeft };

        actionCard.Controls.Add(reloadButton);
        actionCard.Controls.Add(inlineStatus);
        actionCard.Controls.Add(saveButton);
        contentPanel.Controls.Add(actionCard);

        // 底部状态条
        statusBar = new Panel { Dock = DockStyle.Bottom, Height = 40, BackColor = Color.FromArgb(0xFA, 0xFB, 0xFC) };
        statusBar.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(0xE5, 0xE7, 0xEB));
            e.Graphics.DrawLine(pen, 0, 0, statusBar.Width, 0);
        };
        statusDot = new Label { Location = new Point(24, 16), Size = new Size(8, 8), BackColor = Color.FromArgb(0x16, 0xA3, 0x4A) };
        statusDot.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(statusDot.BackColor);
            e.Graphics.FillEllipse(brush, 0, 0, 7, 7);
        };
        statusText = new Label { Text = "就绪", Location = new Point(40, 8), Size = new Size(300, 24), Font = UiFont(11f, FontStyle.Regular), ForeColor = Color.FromArgb(0x16, 0xA3, 0x4A), TextAlign = ContentAlignment.MiddleLeft };
        effectHint = new Label { Text = "白名单变更秒级生效；起始页变更可能需重启 Edge 或下次登录后完全生效", Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(300, 8), Size = new Size(430, 24), Font = UiFont(11f, FontStyle.Regular), ForeColor = textMuted, TextAlign = ContentAlignment.MiddleRight };
        statusBar.Controls.Add(statusDot);
        statusBar.Controls.Add(statusText);
        statusBar.Controls.Add(effectHint);

        Controls.Add(contentPanel);
        Controls.Add(headerPanel);
        Controls.Add(statusBar);

        ResumeLayout(false);

        // 供逻辑代码使用的边框面板引用
        _githubBorder = githubBorder;
        _startBorder = startBorder;

        Label FieldLabel(string text, int x, int y) => new Label
        {
            Text = text, Location = new Point(x, y), AutoSize = true,
            Font = UiFont(12f, FontStyle.Bold), ForeColor = Color.FromArgb(0x37, 0x41, 0x51),
        };

        TextBox FieldInput(int x, int y, RoundPanel border)
        {
            var tb = new TextBox
            {
                Location = new Point(x + 10, y + 6),
                Size = new Size(420, 22),
                BorderStyle = BorderStyle.None,
                Font = UiFont(12f, FontStyle.Regular),
                ForeColor = textDark,
                BackColor = Color.White,
            };
            tb.GotFocus += (_, _) => border.BorderColor = primary;
            tb.LostFocus += (_, _) => border.BorderColor = Color.FromArgb(0xD1, 0xD5, 0xDB);
            return tb;
        }

        Label FieldHint(string text, int x, int y, int w, Color color) => new Label
        {
            Text = text, Location = new Point(x, y), Size = new Size(w, 24),
            Font = UiFont(11f, FontStyle.Regular), ForeColor = color, TextAlign = ContentAlignment.MiddleLeft,
        };

        Label FieldError(int x, int y, string text) => new Label
        {
            Text = text, Location = new Point(x, y), AutoSize = true,
            Font = UiFont(11f, FontStyle.Regular), ForeColor = Color.FromArgb(0xDC, 0x26, 0x26),
        };
    }

    private static Font UiFont(float size, FontStyle style)
    {
        var families = new[] { "思源黑体", "Source Han Sans SC", "Microsoft YaHei UI", "Microsoft YaHei" };
        var name = families.FirstOrDefault(f =>
        {
            try { using var font = new Font(f, size); return font.Name.Equals(f, StringComparison.OrdinalIgnoreCase); }
            catch { return false; }
        }) ?? "Microsoft YaHei UI";
        return new Font(name, size, style, GraphicsUnit.Pixel);
    }

    private RoundPanel _githubBorder = null!;
    private RoundPanel _startBorder = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }
}
