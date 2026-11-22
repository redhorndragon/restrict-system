# RestrictSystem — Windows 家庭版严格 Kiosk 访问限制

面向 Windows 10/11 家庭版的网站访问限制软件。管理员一次性配置后，指定标准用户登录时进入受控环境：无桌面/任务栏，仅 Edge 最大化窗口运行，保留前进/后退/主页按钮且网站登录状态可保持（非 InPrivate），只能访问从 GitHub Pages 页面解析出的白名单链接。

## 组件

- `KioskGuard.Service`：以 LocalSystem 运行的 Windows 服务，负责抓取白名单、下发策略、守护进程，并热加载配置。
- `KioskLauncher`：目标用户的自定义 Winlogon Shell，提供全屏宿主窗口、Edge 看护与热键屏蔽。
- `KioskSetup`：管理员一次性配置工具，设置 Shell、写配置、安装服务。
- `KioskAdmin`：管理员图形界面，修改白名单源（GitHub Pages 地址、起始页、刷新周期），并可增删"额外允许的网址"（与 GitHub Pages 白名单合并），保存后秒级生效。强制 UAC 提权，仅管理员可运行。
- `KioskGuard.Config`：配置模型与文件读写共享库。

## 快速开始

1. 本仓库已包含白名单承载页 `rules/allowlist.html`，发布后的地址为：
   `https://redhorndragon.github.io/restrict-system/rules/allowlist.html`
   （在 GitHub 仓库 Settings → Pages 中将 Source 设为 `main` 分支根目录即可）。
   编辑该文件的 `kiosk-allowlist` 脚本块即可维护允许的链接。
2. 以管理员身份运行：
   ```powershell
   .\scripts\install.ps1 -User "study" -GitHubPagesUrl "https://redhorndragon.github.io/restrict-system/rules/allowlist.html"
   ```
3. 重启并使目标用户登录。
4. 管理员账户（如 gbr）可从开始菜单运行「Kiosk Access Management」，修改白名单源；保存后服务在数秒内自动刷新，无需重启。起始页变更可能需重启 Edge 或下次登录后完全生效。

卸载：
```powershell
.\scripts\uninstall.ps1 -User "KioskUser"
```

### 浏览器运行方式

KioskLauncher 以普通最大化窗口（非 Edge kiosk/InPrivate）启动 Edge，这样可以保留网站登录状态并显示前进/后退/主页按钮。限制由以下机制共同实现：

- Edge 策略：`URLBlocklist=*`、`URLAllowlist`（白名单网址）、`AddressBarIsReadOnly=1`（地址栏只读）、`IncognitoModeAvailability=2`（禁用 InPrivate）、`ShowHomeButton=1`、`DeveloperToolsAvailability=2`
- KioskLauncher：屏蔽 Win/Alt+Tab/Alt+F4/Ctrl+Esc 等热键；Edge 退出后约 2.5 秒自动重启
- KioskGuard 服务：结束目标用户的非白名单进程，禁用任务管理器

注意：用户仍可点击标签栏的关闭按钮或新建标签页；新标签页打开的是白名单起始页，访问非白名单网址会被阻止。关闭 Edge 后会被 watchdog 自动重启。

详见 `docs/DEPLOYMENT.md`；安全边界见 `docs/LIMITATIONS.md`。

## 白名单页面格式

支持两种方式（优先方式一）：

**方式一：JSON manifest**
```html
<script type="application/json" id="kiosk-allowlist">
[
  "https://example.com/page1",
  "https://example.com/page2"
]
</script>
```
也支持 `[{"url": "https://..."}]` 形式。

**方式二：容器链接**
```html
<div id="kiosk-allowlist">
  <a href="https://example.com/page1">页面1</a>
  <a href="/relative">相对路径</a>
</div>
```

仅允许 `http`/`https` 链接；相对路径按页面 URL 解析；`javascript:`、`mailto:`、`data:` 等被忽略。抓取失败或解析为空时保留上一次有效白名单（fail-closed）。

仓库中的 `rules/allowlist.html` 采用方式一，页面可见列表由脚本从 JSON manifest 渲染，无需手动同步两处内容。

**B 站 UP 主规则**：在数组中使用 `"bilibili:up=23630128"`（换成实际 UP 主 ID），服务会爬取 `https://space.bilibili.com/{ID}/video`，从页面内嵌数据提取视频 BV 号并放行对应视频链接，同时始终放行该 UP 主空间页本身。不处理 `live.bilibili.com` 直播间。注意：B 站视频列表由 JS 动态加载，静态爬取依赖页面内嵌数据，可能不完整或受反爬影响；爬取失败时仅放行空间页（降级），不影响其他白名单条目。详见 `docs/LIMITATIONS.md`。

此外，管理员可在 `KioskAdmin` 界面的"额外允许的网址"卡片中增删 URL，这些 URL 保存在本机 `config.json` 的 `ExtraAllowedUrls` 字段，服务抓取 GitHub Pages 白名单后会与其合并。适用于临时追加允许地址而无需修改 GitHub 页面。
