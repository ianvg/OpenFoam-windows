param(
    [Parameter(Mandatory)][string]$Setup,
    [switch]$Uninstall
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$smokeRoot = Join-Path $projectRoot 'artifacts\installer-smoke'
$installRoot = Join-Path $smokeRoot 'installed app'
$registryPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{E045D29F-4461-4DB0-A1FC-53CBF945B93C}_is1'
$marker = Join-Path $installRoot 'preserve-user-file.txt'

if ($Uninstall) {
    $registered = Get-ItemProperty -LiteralPath $registryPath
    if ($registered.InstallLocation.TrimEnd('\') -ne $installRoot) { throw 'Refusing to uninstall a different installation.' }
    $uninstaller = Join-Path $installRoot 'unins000.exe'
    $process = Start-Process -FilePath $uninstaller -ArgumentList @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', "/LOG=`"$smokeRoot\uninstall.log`"") -Wait -PassThru -WindowStyle Hidden
    if ($process.ExitCode -ne 0) { throw "Uninstall failed: $($process.ExitCode)" }
    if (Test-Path -LiteralPath (Join-Path $installRoot 'FoamStudio.App.exe')) { throw 'Uninstall left the app executable.' }
    if (Test-Path -LiteralPath $registryPath) { throw 'Uninstall left its registration.' }
    if ((Get-Content -LiteralPath $marker -Raw).Trim() -ne 'User files must survive uninstall.') { throw 'Uninstall removed or changed a user file.' }
    Write-Output 'PASS: uninstall removed application files/registration and preserved the user file.'
    return
}

if (Test-Path -LiteralPath $registryPath) { throw 'An installation already exists; refusing to replace it during a test.' }
if (Test-Path -LiteralPath $installRoot) { throw 'Use a fresh smoke-test directory.' }
New-Item -ItemType Directory -Path $smokeRoot -Force | Out-Null
$resolvedSetup = (Resolve-Path -LiteralPath $Setup).Path
$process = Start-Process -FilePath $resolvedSetup -ArgumentList @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', "/DIR=`"$installRoot`"", "/LOG=`"$smokeRoot\install.log`"") -Wait -PassThru -WindowStyle Hidden
if ($process.ExitCode -ne 0) { throw "Install failed: $($process.ExitCode)" }
foreach ($name in @('FoamStudio.App.exe', 'coreclr.dll', 'Microsoft.UI.Xaml.dll', 'ThirdPartyNotices\dependency-inventory.json', 'SETUP-NOTES.txt', 'unins000.exe')) {
    if (-not (Test-Path -LiteralPath (Join-Path $installRoot $name))) { throw "Installed payload missing $name" }
}
$registered = Get-ItemProperty -LiteralPath $registryPath
if ($registered.InstallLocation.TrimEnd('\') -ne $installRoot) { throw 'Install location was not registered correctly.' }
$shortcut = Join-Path ([Environment]::GetFolderPath('Programs')) 'Foam Studio.lnk'
if (-not (Test-Path -LiteralPath $shortcut)) { throw 'Start menu shortcut is missing.' }
'User files must survive uninstall.' | Set-Content -LiteralPath $marker
Write-Output "PASS: per-user installation, runtime files, notices, registration, and Start menu shortcut. Launch $installRoot\FoamStudio.App.exe for interactive verification, then rerun with -Uninstall."
