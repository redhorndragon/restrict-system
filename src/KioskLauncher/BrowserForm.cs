using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace KioskLauncher;

public sealed class BrowserForm : Form
{
    // 禁用 B 站列表页的 hover 预览；视频播放页不受影响。
    private const string DisableBiliHoverPreviewScript = """
        (function () {
          'use strict';
          function init() {
            var host = location.hostname || '';
            var path = location.pathname || '';
            if (!/(^|\.)bilibili\.com$/.test(host)) return;
            if (/^\/video\//.test(path)) return;

            function stop(v) {
              try { v.pause(); v.currentTime = 0; } catch (e) {}
              try { v.removeAttribute('src'); v.load(); } catch (e) {}
            }

            var nativePlay = HTMLMediaElement.prototype.play;
            HTMLMediaElement.prototype.play = function () {
              var result = nativePlay.apply(this, arguments);
              try { this.pause(); } catch (e) {}
              return result;
            };

            var style = document.createElement('style');
            style.textContent = 'video { display: none !important; }';
            (document.head || document.documentElement).appendChild(style);

            var observer = new MutationObserver(function (records) {
              for (var i = 0; i < records.length; i++) {
                var added = records[i].addedNodes;
                for (var j = 0; j < added.length; j++) {
                  var node = added[j];
                  if (node.nodeType !== 1) continue;
                  if (node.tagName === 'VIDEO') stop(node);
                  if (node.querySelectorAll) {
                    var videos = node.querySelectorAll('video');
                    for (var k = 0; k < videos.length; k++) stop(videos[k]);
                  }
                }
              }
            });
            observer.observe(document, { childList: true, subtree: true });
          }
          if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
          else init();
        })();
        """;

    private readonly BrowserPolicyStore _policyStore;
    private readonly string _startUrl;
    private readonly Action<string> _log;
    private readonly TabControl _tabs;
    private readonly TextBox _address;
    private readonly Button _back;
    private readonly Button _forward;
    private readonly Button _home;
    private CoreWebView2Environment? _env;

    public BrowserForm(BrowserPolicyStore policyStore, string startUrl, Action<string> log)
    {
        _policyStore = policyStore;
        _startUrl = startUrl;
        _log = log;

        Text = "Kiosk Browser";
        WindowState = FormWindowState.Maximized;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        Bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
        BackColor = SystemColors.Control;

        var toolbar = new Panel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(6, 6, 6, 4) };
        _back = new Button { Text = "‹", Size = new Size(34, 30), Location = new Point(6, 6) };
        _forward = new Button { Text = "›", Size = new Size(34, 30), Location = new Point(44, 6) };
        _home = new Button { Text = "⌂", Size = new Size(34, 30), Location = new Point(82, 6) };
        var refresh = new Button { Text = "⟳", Size = new Size(34, 30), Location = new Point(120, 6) };
        var newTab = new Button { Text = "+", Size = new Size(34, 30), Location = new Point(158, 6) };
        _address = new TextBox
        {
            ReadOnly = true,
            Location = new Point(200, 8),
            Size = new Size(Math.Max(200, Bounds.Width - 220), 26),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Color.White,
        };
        _back.Click += (_, _) => ActiveTab?.WebView.GoBack();
        _forward.Click += (_, _) => ActiveTab?.WebView.GoForward();
        _home.Click += (_, _) => NavigateActive(_startUrl);
        refresh.Click += (_, _) => ActiveTab?.WebView.Reload();
        newTab.Click += (_, _) => _ = AddTabAsync(_startUrl);
        toolbar.Controls.AddRange([_back, _forward, _home, refresh, newTab, _address]);

        _tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Padding = new Point(8, 4),
            DrawMode = TabDrawMode.OwnerDrawFixed,
            SizeMode = TabSizeMode.Fixed,
            ItemSize = new Size(180, 26),
        };
        _tabs.DrawItem += Tabs_DrawItem;
        _tabs.MouseDown += Tabs_MouseDown;
        _tabs.SelectedIndexChanged += (_, _) => UpdateAddress();
        Controls.Add(_tabs);
        Controls.Add(toolbar);
    }

    private BrowserTab? ActiveTab => _tabs.SelectedTab?.Tag as BrowserTab;

    private static Rectangle GetCloseButtonBounds(Rectangle tabBounds) =>
        new(tabBounds.Right - 20, tabBounds.Top + (tabBounds.Height - 16) / 2, 16, 16);

    private void Tabs_DrawItem(object? sender, DrawItemEventArgs e)
    {
        var bounds = _tabs.GetTabRect(e.Index);
        var selected = (e.State & DrawItemState.Selected) != 0;
        using var background = new SolidBrush(selected ? SystemColors.Control : SystemColors.ControlDark);
        e.Graphics.FillRectangle(background, bounds);

        var close = GetCloseButtonBounds(bounds);
        var textBounds = new Rectangle(bounds.Left + 6, bounds.Top, close.Left - bounds.Left - 8, bounds.Height);
        TextRenderer.DrawText(e.Graphics, _tabs.TabPages[e.Index].Text, _tabs.Font, textBounds,
            selected ? SystemColors.ControlText : SystemColors.GrayText,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(e.Graphics, "×", _tabs.Font, close,
            selected ? Color.Firebrick : Color.DimGray,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    private void Tabs_MouseDown(object? sender, MouseEventArgs e)
    {
        for (var i = 0; i < _tabs.TabPages.Count; i++)
        {
            if (!GetCloseButtonBounds(_tabs.GetTabRect(i)).Contains(e.Location)) continue;
            CloseTab(i);
            return;
        }
    }

    private void CloseTab(int index)
    {
        var page = _tabs.TabPages[index];
        var tab = page.Tag as BrowserTab;
        _tabs.TabPages.RemoveAt(index);
        tab?.WebView.Dispose();

        if (_tabs.TabPages.Count == 0)
            _ = AddTabAsync(_startUrl);
        else
            UpdateAddress();
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        try
        {
            var dataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "KioskGuard", "WebView2");
            Directory.CreateDirectory(dataDir);
            _env = await CoreWebView2Environment.CreateAsync(userDataFolder: dataDir);
            await AddTabAsync(_startUrl);
        }
        catch (Exception ex)
        {
            _log($"WebView2 初始化失败：{ex.Message}");
            MessageBox.Show(this, $"浏览器初始化失败：{ex.Message}\n请确认已安装 Edge WebView2 Runtime。",
                "Kiosk Browser", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task AddTabAsync(string url, string? referer = null)
    {
        var page = new TabPage("新标签页");
        var web = new WebView2 { Dock = DockStyle.Fill };
        page.Controls.Add(web);
        _tabs.TabPages.Add(page);
        _tabs.SelectedTab = page;

        var tab = new BrowserTab(page, web) { CurrentUrl = referer };
        page.Tag = tab;
        try
        {
            await web.EnsureCoreWebView2Async(_env);
            web.CoreWebView2.Settings.AreDevToolsEnabled = false;
            web.CoreWebView2.Settings.IsStatusBarEnabled = false;
            await web.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(DisableBiliHoverPreviewScript);
            web.CoreWebView2.NavigationStarting += (_, args) =>
            {
                var referer = tab.CurrentUrl;
                if (!UrlPolicy.IsAllowed(args.Uri, _policyStore.Current, referer))
                {
                    args.Cancel = true;
                    _log($"已阻止导航：{args.Uri}（来源：{referer ?? "无"}）");
                }
                else
                {
                    tab.PendingUrl = args.Uri;
                }
            };
            web.CoreWebView2.NavigationCompleted += (_, args) =>
            {
                if (args.IsSuccess && tab.PendingUrl != null)
                {
                    tab.CurrentUrl = tab.PendingUrl;
                    page.Text = TrimTitle(tab.PendingUrl);
                    tab.PendingUrl = null;
                    UpdateAddress();
                }
            };
            web.CoreWebView2.NewWindowRequested += (_, args) =>
            {
                args.Handled = true;
                var referer = tab.CurrentUrl;
                if (UrlPolicy.IsAllowed(args.Uri, _policyStore.Current, referer))
                    _ = AddTabAsync(args.Uri, referer);
                else
                    _log($"已阻止新窗口：{args.Uri}（来源：{referer ?? "无"}）");
            };
            web.Source = new Uri(url);
        }
        catch (Exception ex)
        {
            _log($"标签页初始化失败：{ex.Message}");
        }
    }

    private void NavigateActive(string url)
    {
        if (ActiveTab is { } tab && tab.WebView.CoreWebView2 != null)
            tab.WebView.Source = new Uri(url);
    }

    private void UpdateAddress()
    {
        _address.Text = ActiveTab?.CurrentUrl ?? string.Empty;
        _back.Enabled = ActiveTab?.WebView.CanGoBack == true;
        _forward.Enabled = ActiveTab?.WebView.CanGoForward == true;
    }

    private static string TrimTitle(string url)
    {
        if (url.Length <= 24) return url;
        return url[..21] + "...";
    }

    private sealed class BrowserTab(TabPage page, WebView2 webView)
    {
        public TabPage Page { get; } = page;
        public WebView2 WebView { get; } = webView;
        public string? CurrentUrl { get; set; }
        public string? PendingUrl { get; set; }
    }
}
