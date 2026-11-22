# 安全边界与已知限制

本方案面向 Windows 家庭版的“合理限制”场景，不能等同于企业级终端管控。请如实评估以下边界。

## 已覆盖

- 目标用户登录后加载自定义 Shell（KioskLauncher）而非 explorer.exe，桌面/任务栏不出现。
- KioskLauncher 是基于 WebView2 的自研浏览器，普通（非无痕）持久化 profile，可保留网站登录状态；多标签、地址栏只读。
- 屏蔽 Win、Alt+Tab、Ctrl+Esc、Ctrl+Shift+Esc、Alt+F4 等常用切出热键。
- 禁用任务管理器（目标用户策略）。
- 浏览器在导航发起时（`NavigationStarting` / `NewWindowRequested`）按策略文件仅放行白名单网址，非白名单直接取消导航；不再依赖 Edge 注册表策略。
- 白名单、起始页与 B 站 UP 主 ID 写入 `C:\ProgramData\KioskGuard\browser-policy.json`，浏览器定时热加载；策略文件缺失或无效时降级为仅允许起始页（fail-closed）。
- SYSTEM 服务监听进程启动并定时扫描，终止目标用户的非白名单进程（仅保留 `msedgewebview2.exe` 与 `KioskLauncher.exe`，不含 `msedge.exe`）。
- 白名单抓取失败时保留上次有效集合（fail-closed），不会整体放行。
- 程序与数据目录收紧 ACL，标准用户无法修改。
- 管理界面 `KioskAdmin.exe` 通过 `requireAdministrator` 清单强制 UAC 提权，并在启动时二次校验管理员角色；标准用户无法运行。

## 不能覆盖 / 已知绕过

- **Ctrl+Alt+Del（SAS）安全注意序列**：由 Winlogon 在安全桌面处理，用户态无法屏蔽。禁用任务管理器后，仍可能看到“锁定/注销/更改密码”等选项；彻底限制需自定义 Credential Provider（Credential Guard / 第三方 GINA 替代），不在本方案范围内。
- **安全模式 / 高级启动选项**：能物理接触机器并进入高级启动选项的管理员可绕过。
- **获知管理员密码**：任何知道本机管理员凭据的人都可运行管理界面、卸载服务、还原 Shell。管理界面以 UAC 为唯一门槛，任何管理员均可运行，不区分具体管理员账户。
- **离线挂载磁盘**：将系统盘挂载到其他系统可直接修改注册表和文件。
- **硬件级手段**：如使用其他启动介质、BIOS/UEFI 设置等。
- **WebView2 / 浏览器内部能力**：自研浏览器未禁用 WebView2 内置的开发工具或内部 scheme（如 `edge://`、`devtools://`），如这些入口可被用户触发，可能成为绕过点；当前仅拦截 `http/https` 导航，非 http/https 一律阻止。
- **白名单页面本身**：若 GitHub Pages 页面包含跳转到非白名单地址的链接，该地址不会被自动加入白名单，因此会被拦截；需管理员维护页面内容。
- **B 站来源链放行的局限**：从白名单中的 UP 主空间页（如 `space.bilibili.com/{UPID}`）点击跳转到 `www.bilibili.com/video/BV...` 会自动放行；但直接打开视频 URL、从非白名单页面跳转、或来源链中 referer 丢失时仍会被拦截。该规则基于导航来源，不解析 WBI 签名 API，也不做服务端静态爬取，因此不依赖 B 站接口变更，但同样不保证覆盖所有入口（如直播间、嵌入播放页、分享短链）。
- **家庭版限制**：无 AppLocker/WDAC，进程守护基于进程名与用户 SID，恶意程序若伪装成白名单进程名（如 `msedgewebview2.exe`）可能逃避检测；请保持目录 ACL 与系统更新。
- **策略文件 ACL**：目标用户对 `browser-policy.json` 仅有读取权限；任何能写入该文件（管理员凭据）的人都能改写白名单。

## 建议的补充加固

- 为 BIOS/UEFI 与 Windows 管理员账户设置强密码。
- 启用 BitLocker 并配置 TPM + PIN。
- 若机器专供该用户使用，可通过组策略/注册表进一步限制 WebView2 运行时与其他浏览器，防止用户切换浏览器。
- 定期审计 GitHub Pages 页面与本机日志。
- 如有更高安全需求，考虑 Windows 专业版/企业版的分配访问（Assigned Access）、AppLocker/WDAC 与自定义 Credential Provider。
