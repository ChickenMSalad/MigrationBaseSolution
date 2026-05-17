$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p1-set034-release-manifest"

Write-Host "Applying P1 Set 034 from $repoRoot"

$files = @(
    "tools\release\new-release-manifest.ps1",
    "tools\release\new-release-manifest.cmd",
    "docs\azure\RELEASE_MANIFEST.md",
    "docs\cloud-roadmap-cleanup\P1_SET_034_RELEASE_MANIFEST.md"
)

foreach ($relative in $files) {
    $source = Join-Path $payloadRoot $relative
    $target = Join-Path $repoRoot $relative
    $targetDirectory = Split-Path $target -Parent

    if (!(Test-Path $source)) {
        throw "Drop-in package is missing expected file: $source"
    }

    if (!(Test-Path $targetDirectory)) {
        New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
    }

    Copy-Item $source $target -Force
    Write-Host "Verified $relative"
}

Write-Host ""
Write-Host "P1 Set 034 applied."
Write-Host "Validation:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\release\new-release-manifest.ps1 -Version 0.1.0-dev -EnvironmentName dev"
