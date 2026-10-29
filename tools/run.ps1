$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$appPath = Join-Path $projectRoot 'src\FoamStudio.App\bin\x64\Debug\net10.0-windows10.0.26100\win-x64\FoamStudio.App.exe'
if (-not (Test-Path -LiteralPath $appPath)) { throw 'Build first with ./tools/build.ps1.' }
# This launches the interactive application window.
Start-Process -FilePath $appPath -WorkingDirectory (Split-Path $appPath -Parent)
