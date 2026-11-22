#Requires -RunAsAdministrator
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$launcherSrc = Join-Path $root '.publish\launcher'
$installDir = 'C:\Program Files\KioskGuard'

Write-Host '== Stopping KioskLauncher =='
Get-Process KioskLauncher -ErrorAction SilentlyContinue | Stop-Process -Force

Write-Host '== Replacing launcher files =='
Copy-Item (Join-Path $launcherSrc 'KioskLauncher.dll') $installDir -Force

Write-Host 'Done.'
