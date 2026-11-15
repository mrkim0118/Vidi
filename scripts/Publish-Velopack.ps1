param(
    [string]$Version = "1.0.0",
    [string]$PackId = "Vidi",
    [string]$PackTitle = "Vidi",
    [string]$Channel = "win",
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release",
    [string]$MainExe = "Vidi.exe",
    [string]$Authors = "Manggdy",
    [string]$DefaultUpdateSource = "https://tech.trendarchive.io/Utility/VideoPlayer/",
    [string]$PublishDir = "",
    [string]$OutputDir = "",
    [string]$ReleaseNotes = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Resolve-FullPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$BasePath,
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        throw "Path value cannot be empty."
    }

    if ([System.IO.Path]::IsPathRooted($Value)) {
        return [System.IO.Path]::GetFullPath($Value)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $BasePath $Value))
}

function Convert-ToAssemblyVersion {
    param([Parameter(Mandatory = $true)][string]$SemanticVersion)

    $coreVersion = $SemanticVersion.Split('-', 2)[0]
    $parts = @($coreVersion.Split('.', [System.StringSplitOptions]::RemoveEmptyEntries))
    while ($parts.Count -lt 4) {
        $parts += "0"
    }

    if ($parts.Count -gt 4) {
        $parts = $parts[0..3]
    }

    return ($parts -join '.')
}

function Convert-ToFileSafeName {
    param([Parameter(Mandatory = $true)][string]$Value)

    $safe = $Value -replace '[^A-Za-z0-9]+', '-'
    return $safe.Trim('-')
}

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-FullPath -BasePath $scriptRoot -Value ".."
$projectPath = Resolve-FullPath -BasePath $repoRoot -Value "Vidi.csproj"

if ([string]::IsNullOrWhiteSpace($PublishDir)) {
    $PublishDir = "artifacts\publish\$Version\$Runtime"
}

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = "artifacts\velopack\Releases"
}

if ([string]::IsNullOrWhiteSpace($ReleaseNotes)) {
    $ReleaseNotes = "artifacts\velopack\release-notes-$Version.md"
}

$publishDirPath = Resolve-FullPath -BasePath $repoRoot -Value $PublishDir
$outputDirPath = Resolve-FullPath -BasePath $repoRoot -Value $OutputDir
$releaseNotesPath = Resolve-FullPath -BasePath $repoRoot -Value $ReleaseNotes
$assemblyVersion = Convert-ToAssemblyVersion -SemanticVersion $Version

New-Item -ItemType Directory -Force -Path $publishDirPath | Out-Null
New-Item -ItemType Directory -Force -Path $outputDirPath | Out-Null
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $releaseNotesPath) | Out-Null

if (Test-Path $publishDirPath) {
    Get-ChildItem -LiteralPath $publishDirPath -Force | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
}

if (-not (Test-Path $releaseNotesPath)) {
    @(
        "# Vidi $Version"
        ""
        "- Initial Velopack release."
    ) | Set-Content -Path $releaseNotesPath -Encoding UTF8
}

Push-Location $repoRoot
try {
    dotnet tool restore --ignore-failed-sources
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet tool restore failed."
    }

    dotnet restore $projectPath -r $Runtime --ignore-failed-sources
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet restore failed."
    }

    dotnet publish $projectPath `
        -c $Configuration `
        -r $Runtime `
        --self-contained true `
        --no-restore `
        -p:Version=$Version `
        -p:AssemblyVersion=$assemblyVersion `
        -p:FileVersion=$assemblyVersion `
        -p:InformationalVersion=$Version `
        -p:PublishSingleFile=false `
        -o $publishDirPath
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed."
    }

    Get-ChildItem -LiteralPath $publishDirPath -Filter "*.pdb" -File -ErrorAction SilentlyContinue |
        Remove-Item -Force -ErrorAction SilentlyContinue

    dotnet tool run vpk pack `
        --packId $PackId `
        --packVersion $Version `
        --packDir $publishDirPath `
        --mainExe $MainExe `
        --packAuthors $Authors `
        --packTitle $PackTitle `
        --channel $Channel `
        --runtime $Runtime `
        --releaseNotes $releaseNotesPath `
        --outputDir $outputDirPath `
        --shortcuts "Desktop,StartMenuRoot"
    if ($LASTEXITCODE -ne 0) {
        throw "vpk pack failed."
    }
}
finally {
    Pop-Location
}

$setupFileName = "$PackId-$Channel-Setup.exe"
$setupFilePath = Join-Path $outputDirPath $setupFileName
if (Test-Path $setupFilePath) {
    $versionedSetupName = "{0}-Setup-{1}.exe" -f (Convert-ToFileSafeName -Value $PackTitle), $Version
    $versionedSetupPath = Join-Path $outputDirPath $versionedSetupName
    Copy-Item -LiteralPath $setupFilePath -Destination $versionedSetupPath -Force
}

Write-Host "Velopack release complete."
Write-Host "Publish files : $publishDirPath"
Write-Host "Release feed  : $outputDirPath"
Write-Host "Update source : $DefaultUpdateSource"
