param([string]$Version = '0.1.0')
$ErrorActionPreference = 'Stop'
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw 'Use a numeric version such as 0.1.0.' }
$projectRoot = Split-Path $PSScriptRoot -Parent
$releaseRoot = Join-Path $projectRoot "artifacts\release\$Version"
if (Test-Path -LiteralPath $releaseRoot) { throw "Use a new output directory; already exists: $releaseRoot" }
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (-not (Test-Path -LiteralPath $vswhere)) { throw 'Visual Studio build tools are required.' }
$msbuild = & $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
$compiler = @(
    (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'),
    (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
    (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe')
) | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $msbuild -or -not $compiler) { throw 'MSBuild and Inno Setup 6 are required.' }
$payload = Join-Path $releaseRoot 'app'
New-Item -ItemType Directory -Path $payload -Force | Out-Null
Push-Location $projectRoot
try {
    dotnet test tests/FoamStudio.Tests/FoamStudio.Tests.csproj -c Release
    if ($LASTEXITCODE -ne 0) { throw "Tests failed: $LASTEXITCODE" }
    & $msbuild src/FoamStudio.App/FoamStudio.App.csproj /restore /t:Publish /p:Configuration=Release /p:Platform=x64 /p:PublishProfile=ReleaseWindows "/p:Version=$Version" "/p:ApplicationDisplayVersion=$Version" "/p:PublishDir=$payload\" /verbosity:minimal /nologo
    if ($LASTEXITCODE -ne 0) { throw "Publish failed: $LASTEXITCODE" }
    foreach ($required in @('FoamStudio.App.exe', 'coreclr.dll', 'Microsoft.UI.Xaml.dll')) {
        if (-not (Test-Path -LiteralPath (Join-Path $payload $required))) { throw "Self-contained payload is missing $required" }
    }
    Copy-Item LICENSE,THIRD_PARTY_NOTICES.md -Destination $payload
    Copy-Item installer/Prerequisites.txt -Destination (Join-Path $payload 'SETUP-NOTES.txt')
    & "$PSScriptRoot/collect-notices.ps1" -Payload $payload
    Copy-Item -LiteralPath (Join-Path (Split-Path $compiler -Parent) 'license.txt') -Destination (Join-Path $payload 'ThirdPartyNotices\InnoSetup-license.txt')
    & $compiler "/DPayloadDir=$payload" "/DOutputDir=$releaseRoot" "/DAppVersion=$Version" installer/FoamStudio.iss
    if ($LASTEXITCODE -ne 0) { throw "Installer compilation failed: $LASTEXITCODE" }
    $setup = Join-Path $releaseRoot "Foam-Studio-$Version-win-x64-Setup.exe"
    $hash = (Get-FileHash -LiteralPath $setup -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $(Split-Path $setup -Leaf)" | Set-Content (Join-Path $releaseRoot 'SHA256SUMS.txt') -Encoding ascii
    Write-Output "Installer: $setup"
} finally { Pop-Location }
