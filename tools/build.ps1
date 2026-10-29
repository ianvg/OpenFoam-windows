param([switch]$Test)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (-not (Test-Path -LiteralPath $vswhere)) { throw 'Install Visual Studio with the WinUI application development workload.' }
$msbuild = & $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (-not $msbuild) { throw 'MSBuild was not found in Visual Studio.' }
Push-Location $projectRoot
try {
    & $msbuild src/FoamStudio.App/FoamStudio.App.csproj /restore /p:Configuration=Debug /p:Platform=x64 /verbosity:minimal /nologo
    if ($LASTEXITCODE -ne 0) { throw "Windows build failed: $LASTEXITCODE" }
    if ($Test) {
        dotnet test tests/FoamStudio.Tests/FoamStudio.Tests.csproj -c Release
        if ($LASTEXITCODE -ne 0) { throw "Tests failed: $LASTEXITCODE" }
    }
} finally { Pop-Location }
