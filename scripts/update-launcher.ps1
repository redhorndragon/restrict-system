#Requires -RunAsAdministrator
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$launcherSrc = Join-Path $root '.publish\launcher'
$installDir = 'C:\Program Files\KioskGuard'

Write-Host '== Stopping KioskLauncher =='
Get-Process KioskLauncher -ErrorAction SilentlyContinue | Stop-Process -Force

# KioskLauncher 与服务共用同一目录下的 KioskGuard.Config.dll，需先停服务释放文件锁。
Write-Host '== Stopping KioskGuard service =='
& sc.exe stop KioskGuard 2>$null | Out-Null
$waited = 0
while ($waited -lt 30) {
    $svc = Get-CimInstance Win32_Service -Filter "Name='KioskGuard'" -ErrorAction SilentlyContinue
    $proc = Get-Process -Name 'KioskGuard.Service' -ErrorAction SilentlyContinue
    if (($svc -eq $null -or $svc.State -eq 'Stopped') -and $proc -eq $null) { break }
    if ($proc -ne $null) { $proc | Stop-Process -Force -ErrorAction SilentlyContinue }
    Start-Sleep -Seconds 1
    $waited++
}

Write-Host '== Replacing launcher files =='
foreach ($file in Get-ChildItem -LiteralPath $launcherSrc -File) {
    if ($file.Name -in @('launcher.json', 'config.json')) { continue }
    Copy-Item -LiteralPath $file.FullName -Destination $installDir -Force
}

Write-Host '== Restarting service =='
& sc.exe start KioskGuard 2>$null | Out-Null
& sc.exe query KioskGuard | Select-String 'STATE'
Write-Host 'Done.'
