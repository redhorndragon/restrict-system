#Requires -RunAsAdministrator
param(
    [Parameter(Mandatory=$true)][string]$User,
    [string]$InstallDir = 'C:\Program Files\KioskGuard'
)

$ErrorActionPreference = 'Continue'
$serviceName = 'KioskGuard'
$dataDir = 'C:\ProgramData\KioskGuard'

Write-Host '== 停止并删除服务 =='
& sc.exe stop $serviceName 2>$null
& sc.exe delete $serviceName 2>$null

Write-Host '== 还原用户 Shell =='
$sid = ([System.Security.Principal.NTAccount]".\$User").Translate([System.Security.Principal.SecurityIdentifier]).Value
$winlogonPath = "Registry::HKEY_USERS\$sid\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon"
$backupPath = Join-Path $dataDir 'shell-backup.txt'
$original = if (Test-Path $backupPath) { (Get-Content $backupPath -Raw).Trim() } else { 'explorer.exe' }
if (Test-Path $winlogonPath) {
    Set-ItemProperty -Path $winlogonPath -Name 'Shell' -Value $original
}

Write-Host '== 清除策略 =='
$edgePolicy = "Registry::HKEY_USERS\$sid\SOFTWARE\Policies\Microsoft\Edge"
if (Test-Path $edgePolicy) {
    Remove-Item -Path $edgePolicy -Recurse -Force
}
$systemPolicy = "Registry::HKEY_USERS\$sid\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"
if (Test-Path $systemPolicy) {
    Remove-ItemProperty -Path $systemPolicy -Name 'DisableTaskMgr' -ErrorAction SilentlyContinue
}

Write-Host '== 删除文件 =='
if (Test-Path $InstallDir) { Remove-Item $InstallDir -Recurse -Force }
if (Test-Path $dataDir) { Remove-Item $dataDir -Recurse -Force }

Write-Host '== 删除开始菜单快捷方式 =='
$shortcutName = 'Kiosk 访问限制管理.lnk'
Get-ChildItem 'C:\Users\*\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\KioskGuard' -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -eq $shortcutName } |
    Remove-Item -Force -ErrorAction SilentlyContinue

Write-Host '卸载完成。请重启以恢复默认桌面。'
