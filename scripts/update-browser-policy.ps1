#Requires -RunAsAdministrator
param(
    [Parameter(Mandatory=$true)][string]$User
)
$ErrorActionPreference = 'Stop'
$dataDir = 'C:\ProgramData\KioskGuard'
$configPath = Join-Path $dataDir 'config.json'
$launcherJson = 'C:\Program Files\KioskGuard\launcher.json'

Write-Host '== Updating config.json =='
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
$config | Add-Member -NotePropertyName 'BrowserPolicyPath' -NotePropertyValue (Join-Path $dataDir 'browser-policy.json') -Force
$allowed = @($config.AllowedProcessNames | Where-Object { $_ -ne 'msedge.exe' })
$config | Add-Member -NotePropertyName 'AllowedProcessNames' -NotePropertyValue $allowed -Force
$config | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $configPath -Encoding UTF8

Write-Host '== Updating launcher.json =='
if (Test-Path $launcherJson) {
    $launcher = Get-Content -LiteralPath $launcherJson -Raw | ConvertFrom-Json
    $launcher.PSObject.Properties.Remove('EdgePath')
    $launcher.PSObject.Properties.Remove('RestartDelayMs')
    $launcher | Add-Member -NotePropertyName 'BrowserPolicyPath' -NotePropertyValue (Join-Path $dataDir 'browser-policy.json') -Force
    $launcher | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $launcherJson -Encoding UTF8
}

Write-Host '== Granting browser policy read access =='
$accountName = if ($User -like '*\*' -or $User -like '*@*') { $User } else { "$env:COMPUTERNAME\$User" }
$sid = ([System.Security.Principal.NTAccount]$accountName).Translate([System.Security.Principal.SecurityIdentifier]).Value
& icacls $dataDir /grant "*${sid}:(OI)(CI)RX" | Out-Null

Write-Host 'Done. Please also run update-service.ps1 and update-launcher.ps1.'
