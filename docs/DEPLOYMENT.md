# 部署指南

## 前置条件

- Windows 10/11 家庭版（专业版及以上也可用，但未使用其内置 Kiosk 能力）。
- 已安装 .NET 8 SDK（安装脚本用于发布）与 .NET 8 运行时（目标机器运行所需）。
- 已创建目标标准用户账户（非管理员）。
- 已发布可公开访问的 GitHub Pages 页面，按 README 中的格式提供白名单。

## 安装

1. 克隆/下载本仓库到任意目录。在 GitHub 仓库 Settings → Pages 中将 Source 设为 `main` 分支根目录，使 `rules/allowlist.html` 可通过
   `https://redhorndragon.github.io/restrict-system/rules/allowlist.html` 访问。
2. 以管理员身份打开 PowerShell：
   ```powershell
   cd <仓库目录>
   .\scripts\install.ps1 -User "study" -GitHubPagesUrl "https://redhorndragon.github.io/restrict-system/rules/allowlist.html"
   ```
3. 可选参数：
   - `-StartUrl`：Edge 起始页（默认同 `-GitHubPagesUrl`）。
   - `-RefreshMinutes`：白名单刷新周期，默认 10 分钟。
   - `-EdgePath`：msedge.exe 路径（默认自动探测）。
   - `-ManifestScriptId` / `-AllowlistContainerId`：页面元素 id。
   - `-InstallDir`：安装目录，默认 `C:\Program Files\KioskGuard`。
4. 脚本会发布全部项目、收紧 ACL、写入配置、设置目标用户 Shell、创建服务并添加管理员快捷方式。
5. 重启计算机，使目标用户登录。

## 管理界面

以管理员账户（如 `gbr`）登录后，从开始菜单「KioskGuard → Kiosk Access Management」启动（也可直接运行安装目录下的 `KioskAdmin.exe`）：

- 程序通过 `app.manifest` 强制 UAC 提权；标准用户 `study` 无法运行，也无法借此解除自身限制。
- 界面显示当前 GitHub Pages 地址、Edge 起始页与刷新周期，可修改后点击「保存并立即生效」。
- 保存采用原子写入，失败不会破坏原配置；输入仅接受 http/https 地址与 1-1440 的刷新周期。
- 白名单与 Edge 网址策略在数秒内生效；起始页变更可能需重启 Edge 或下次登录后完全生效。
- 「从文件重新加载」可放弃未保存的修改，重新读取磁盘配置。

## 验证

- 目标用户登录后应直接进入 Edge 全屏，无桌面和任务栏。
- 尝试访问非白名单网址应被 Edge 拦截。
- 服务日志：`C:\ProgramData\KioskGuard\kioskguard-*.log` 与 Windows 事件日志 `KioskGuard` 源。
- Launcher 日志：`%TEMP%\KioskGuard\launcher.log`。

## 维护白名单

编辑仓库中的 `rules/allowlist.html`，修改 `id="kiosk-allowlist"` 的 `application/json` 脚本块内的 URL 数组（字符串或 `{"url":"..."}` 对象），提交并推送到 `main` 分支。GitHub Pages 更新后，服务会在下一个刷新周期（默认 10 分钟）自动抓取；也可在管理界面点击「保存并立即生效」触发即时刷新。页面可见列表由脚本从该数组渲染，无需另行修改。

## 卸载

```powershell
.\scripts\uninstall.ps1 -User "study"
```

卸载会停止并删除服务、还原用户 Shell 为 `explorer.exe`（从 `shell-backup.txt` 读取）、清除策略并删除安装目录与数据目录。完成后重启。

## 常见问题

- **服务未启动**：检查 `sc query KioskGuard`，查看事件日志；确认 `C:\ProgramData\KioskGuard\config.json` 存在且可读取。
- **白名单为空**：检查 GitHub Pages URL 是否可访问、页面格式是否正确；查看日志中「未从页面解析出任何链接」。
- **Edge 反复重启**：确认 `StartUrl` 有效、Edge 路径正确；查看 `launcher.log`。
- **目标用户配置单元未加载**：安装时该用户不能处于已登录状态；若已登录，先注销后再安装。
- **管理界面提示“配置不存在”**：确认 `C:\ProgramData\KioskGuard\config.json` 存在；若通过环境变量 `KIOSKGUARD_CONFIG` 指定了路径，管理界面与服务需使用相同值。
- **保存成功但页面未更新**：GitHub Pages 有 CDN 缓存，服务按刷新周期重新抓取；可稍候或检查页面内容是否已发布。
