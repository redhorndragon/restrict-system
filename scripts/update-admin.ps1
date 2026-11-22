#Requires -RunAsAdministrator
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$adminSrc = Join-Path $root '.publish\admin'
$serviceSrc = Join-Path $root '.publish\service'
$installDir = 'C:\Program Files\KioskGuard'

Write-Host '== Stopping KioskAdmin =='
Get-Process KioskAdmin -ErrorAction SilentlyContinue | Stop-Process -Force

Write-Host '== Stopping KioskGuard service =='
& sc.exe stop KioskGuard 2>$null | Out-Null

$maxWait = 30
$waited = 0
while ($waited -lt $maxWait) {
    $svc = Get-CimInstance Win32_Service -Filter "Name='KioskGuard'" -ErrorAction SilentlyContinue
    $proc = Get-Process -Name 'KioskGuard.Service' -ErrorAction SilentlyContinue
    if (($svc -eq $null -or $svc.State -eq 'Stopped') -and $proc -eq $null) { break }
    if ($proc -ne $null) { $proc | Stop-Process -Force -ErrorAction SilentlyContinue }
    Start-Sleep -Seconds 1
    $waited++
}
Write-Host "Service stopped after ${waited}s"

Write-Host '== Replacing service files =='
Copy-Item (Join-Path $serviceSrc 'KioskGuard.Service.dll') $installDir -Force
Copy-Item (Join-Path $serviceSrc 'KioskGuard.Config.dll') $installDir -Force

Write-Host '== Replacing admin files =='
Copy-Item (Join-Path $adminSrc 'KioskAdmin.exe') $installDir -Force
Copy-Item (Join-Path $adminSrc 'KioskAdmin.dll') $installDir -Force
Copy-Item (Join-Path $adminSrc 'KioskGuard.Config.dll') $installDir -Force

Write-Host '== Restarting service and admin =='
& sc.exe start KioskGuard 2>$null | Out-Null
Start-Process (Join-Path $installDir 'KioskAdmin.exe')
Write-Host 'Done.'
