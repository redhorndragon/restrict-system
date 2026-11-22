#Requires -RunAsAdministrator
param(
    [Parameter(Mandatory=$true)][string]$User,
    [Parameter(Mandatory=$true)][string]$GitHubPagesUrl,
    [string]$StartUrl = '',
    [string]$ManifestScriptId = 'kiosk-allowlist',
    [string]$AllowlistContainerId = 'kiosk-allowlist',
    [int]$RefreshMinutes = 10,
    [string]$EdgePath = '',
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
    if ($LASTEXITCODE -ne 0) { throw "发布失败：$project" }
}

Write-Host '== 发布项目 =='
Publish-Project 'src/KioskGuard.Service/KioskGuard.Service.csproj' $servicePublish
Publish-Project 'src/KioskLauncher/KioskLauncher.csproj' $launcherPublish
Publish-Project 'src/KioskSetup/KioskSetup.csproj' $setupPublish
Publish-Project 'src/KioskAdmin/KioskAdmin.csproj' $adminPublish

Write-Host '== 安装文件 =='
New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
Copy-Item (Join-Path $servicePublish '*') $InstallDir -Recurse -Force
Copy-Item (Join-Path $launcherPublish '*') $InstallDir -Recurse -Force
Copy-Item (Join-Path $setupPublish '*') $InstallDir -Recurse -Force
Copy-Item (Join-Path $adminPublish '*') $InstallDir -Recurse -Force
New-Item -ItemType Directory -Force -Path $dataDir | Out-Null

Write-Host '== 收紧 ACL =='
& icacls $InstallDir /inheritance:r /grant:r '*S-1-5-18:(OI)(CI)F' '*S-1-5-32-544:(OI)(CI)F' '*S-1-5-32-545:(OI)(CI)RX' | Out-Null
& icacls $dataDir /inheritance:r /grant:r '*S-1-5-18:(OI)(CI)F' '*S-1-5-32-544:(OI)(CI)F' | Out-Null

Write-Host '== 配置目标用户与服务 =='
$setupExe = Join-Path $InstallDir 'KioskSetup.exe'
$launcherExe = Join-Path $InstallDir 'KioskLauncher.exe'
$serviceExe = Join-Path $InstallDir 'KioskGuard.Service.exe'

$setupArgs = @('--user', $User, '--github', $GitHubPagesUrl, '--launcher', $launcherExe, '--service', $serviceExe, '--refresh', $RefreshMinutes, '--manifest-id', $ManifestScriptId, '--container-id', $AllowlistContainerId)
if ($StartUrl) { $setupArgs += @('--start', $StartUrl) }
if ($EdgePath) { $setupArgs += @('--edge', $EdgePath) }
& $setupExe @setupArgs
if ($LASTEXITCODE -ne 0) { throw 'KioskSetup 执行失败' }

Write-Host '== 创建管理员快捷方式 =='
$adminExe = Join-Path $InstallDir 'KioskAdmin.exe'
$startMenuDir = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\KioskGuard'
New-Item -ItemType Directory -Force -Path $startMenuDir | Out-Null
$shortcut = Join-Path $startMenuDir 'Kiosk 访问限制管理.lnk'
$shell = New-Object -ComObject WScript.Shell
$lnk = $shell.CreateShortcut($shortcut)
$lnk.TargetPath = $adminExe
$lnk.WorkingDirectory = $InstallDir
$lnk.Description = '配置 Kiosk 白名单源（需管理员权限）'
$lnk.Save()

Write-Host '安装完成。gbr 可从开始菜单运行“Kiosk 访问限制管理”。请重启并使目标用户登录。'
