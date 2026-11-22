#Requires -RunAsAdministrator
param(
    [Parameter(Mandatory=$true)][string]$User,
    [Parameter(Mandatory=$true)][string]$GitHubPagesUrl,
    [string]$StartUrl = '',
    [string]$ManifestScriptId = 'kiosk-allowlist',
    [string]$AllowlistContainerId = 'kiosk-allowlist',
    [int]$RefreshMinutes = 10,
    [string]$InstallDir = 'C:\Program Files\KioskGuard'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$publish = Join-Path $root '.publish'
$servicePublish = Join-Path $publish 'service'
$launcherPublish = Join-Path $publish 'launcher'
$setupPublish = Join-Path $publish 'setup'
$adminPublish = Join-Path $publish 'admin'
$dataDir = 'C:\ProgramData\KioskGuard'

function Publish-Project($project, $output) {
    if (Test-Path $output) { Remove-Item $output -Recurse -Force }
    & dotnet publish (Join-Path $root $project) -c Release -r win-x64 --self-contained false -o $output
    if ($LASTEXITCODE -ne 0) { throw "Publish failed: $project" }
}

Write-Host '== Publishing projects =='
Publish-Project 'src/KioskGuard.Service/KioskGuard.Service.csproj' $servicePublish
Publish-Project 'src/KioskLauncher/KioskLauncher.csproj' $launcherPublish
Publish-Project 'src/KioskSetup/KioskSetup.csproj' $setupPublish
Publish-Project 'src/KioskAdmin/KioskAdmin.csproj' $adminPublish

Write-Host '== Installing files =='
New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
Copy-Item (Join-Path $servicePublish '*') $InstallDir -Recurse -Force
Copy-Item (Join-Path $launcherPublish '*') $InstallDir -Recurse -Force
Copy-Item (Join-Path $setupPublish '*') $InstallDir -Recurse -Force
Copy-Item (Join-Path $adminPublish '*') $InstallDir -Recurse -Force
New-Item -ItemType Directory -Force -Path $dataDir | Out-Null

Write-Host '== Hardening ACLs =='
& icacls $InstallDir /inheritance:r /grant:r '*S-1-5-18:(OI)(CI)F' '*S-1-5-32-544:(OI)(CI)F' '*S-1-5-32-545:(OI)(CI)RX' | Out-Null
& icacls $dataDir /inheritance:r /grant:r '*S-1-5-18:(OI)(CI)F' '*S-1-5-32-544:(OI)(CI)F' | Out-Null

Write-Host '== Granting browser policy read access =='
$accountName = if ($User -like '*\*' -or $User -like '*@*') { $User } else { "$env:COMPUTERNAME\$User" }
$sid = ([System.Security.Principal.NTAccount]$accountName).Translate([System.Security.Principal.SecurityIdentifier]).Value
& icacls $dataDir /grant "*${sid}:(OI)(CI)RX" | Out-Null

Write-Host '== Configuring target user and service =='
$setupExe = Join-Path $InstallDir 'KioskSetup.exe'
$launcherExe = Join-Path $InstallDir 'KioskLauncher.exe'
$serviceExe = Join-Path $InstallDir 'KioskGuard.Service.exe'

$setupArgs = @('--user', $User, '--github', $GitHubPagesUrl, '--launcher', $launcherExe, '--service', $serviceExe, '--refresh', $RefreshMinutes, '--manifest-id', $ManifestScriptId, '--container-id', $AllowlistContainerId)
if ($StartUrl) { $setupArgs += @('--start', $StartUrl) }
& $setupExe @setupArgs
if ($LASTEXITCODE -ne 0) { throw 'KioskSetup failed' }

Write-Host '== Creating admin shortcut =='
$adminExe = Join-Path $InstallDir 'KioskAdmin.exe'
$startMenuDir = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\KioskGuard'
New-Item -ItemType Directory -Force -Path $startMenuDir | Out-Null
$shortcut = Join-Path $startMenuDir 'Kiosk Access Management.lnk'
$shell = New-Object -ComObject WScript.Shell
$lnk = $shell.CreateShortcut($shortcut)
$lnk.TargetPath = $adminExe
$lnk.WorkingDirectory = $InstallDir
$lnk.Description = 'Configure Kiosk allowlist source (admin required)'
$lnk.Save()

Write-Host 'Installation complete. gbr can run "Kiosk Access Management" from the Start menu. Reboot and let the target user log in.'
