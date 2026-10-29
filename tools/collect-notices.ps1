param([Parameter(Mandatory)][string]$Payload)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$assets = Get-Content (Join-Path $projectRoot 'src\FoamStudio.App\obj\project.assets.json') -Raw | ConvertFrom-Json
$noticesRoot = Join-Path $Payload 'ThirdPartyNotices'
New-Item -ItemType Directory -Path $noticesRoot -Force | Out-Null
$inventory = foreach ($library in $assets.libraries.PSObject.Properties) {
    if ($library.Value.type -ne 'package') { continue }
    $packagePath = $null
    foreach ($folder in $assets.packageFolders.PSObject.Properties.Name) {
        $candidate = Join-Path $folder $library.Value.path
        if (Test-Path -LiteralPath $candidate) { $packagePath = $candidate; break }
    }
    if (-not $packagePath) { throw "Package not found: $($library.Name)" }
    $nuspec = Get-ChildItem -LiteralPath $packagePath -Filter '*.nuspec' | Select-Object -First 1
    if (-not $nuspec) { throw "Package metadata missing: $($library.Name)" }
    [xml]$metadata = Get-Content -LiteralPath $nuspec.FullName -Raw
    $license = $metadata.SelectSingleNode("//*[local-name()='metadata']/*[local-name()='license']")
    $licenseUrl = $metadata.SelectSingleNode("//*[local-name()='metadata']/*[local-name()='licenseUrl']")
    $destination = Join-Path $noticesRoot ($library.Name -replace '/', '-')
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    Copy-Item -LiteralPath $nuspec.FullName -Destination $destination
    # Keep the whole package's license files, including embedded third-party notices.
    $noticeFiles = @(Get-ChildItem -LiteralPath $packagePath -Recurse -File | Where-Object { $_.Name -match '(?i)license|licence|copyright|third.?party.?notices|^notice\.' })
    foreach ($file in $noticeFiles) {
        $relative = [IO.Path]::GetRelativePath($packagePath, $file.FullName)
        $target = Join-Path $destination $relative
        New-Item -ItemType Directory -Force -Path (Split-Path $target -Parent) | Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $target
    }
    if ($license -and $license.type -eq 'file') {
        $declaredPath = Join-Path $packagePath $license.InnerText
        if (-not (Test-Path -LiteralPath $declaredPath)) { throw "Missing declared license: $($library.Name)" }
        $target = Join-Path $destination $license.InnerText
        New-Item -ItemType Directory -Force -Path (Split-Path $target -Parent) | Out-Null
        Copy-Item -LiteralPath $declaredPath -Destination $target -Force
    }
    [pscustomobject]@{ Package = $library.Name; LicenseType = $license.type; License = $license.InnerText; LicenseUrl = $licenseUrl.InnerText }
}
$inventory | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $noticesRoot 'dependency-inventory.json') -Encoding utf8
# Also preserve the runtime pack's notices for the self-contained .NET runtime.
$runtime = Get-Content (Join-Path $Payload 'FoamStudio.App.runtimeconfig.json') -Raw | ConvertFrom-Json
$runtimeVersion = ($runtime.runtimeOptions.includedFrameworks | Where-Object name -EQ 'Microsoft.NETCore.App').version
$runtimePath = $null
foreach ($folder in $assets.packageFolders.PSObject.Properties.Name) {
    $candidate = Join-Path $folder "microsoft.netcore.app.runtime.win-x64/$runtimeVersion"
    if (Test-Path -LiteralPath $candidate) { $runtimePath = $candidate; break }
}
if (-not $runtimePath) { throw "Runtime package not found for $runtimeVersion" }
foreach ($name in @('LICENSE.txt', 'THIRD-PARTY-NOTICES.TXT')) {
    $source = Join-Path $runtimePath $name
    if (-not (Test-Path -LiteralPath $source)) { throw "Runtime notice not found: $source" }
    Copy-Item -LiteralPath $source -Destination (Join-Path $noticesRoot ".NET-$name")
}
