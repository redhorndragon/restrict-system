#Requires -RunAsAdministrator
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$launcherSrc = Join-Path $root '.publish\launcher'
$installDir = 'C:\Program Files\KioskGuard'

Write-Host '== Stopping KioskLauncher =='
Get-Process KioskLauncher -ErrorAction SilentlyContinue | Stop-Process -Force

Write-Host '== Replacing launcher files =='
foreach ($file in Get-ChildItem -LiteralPath $launcherSrc -File) {
    if ($file.Name -in @('launcher.json', 'config.json')) { continue }
    Copy-Item -LiteralPath $file.FullName -Destination $installDir -Force
}

Write-Host 'Done.'
