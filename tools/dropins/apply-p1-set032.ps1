$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p1-set032-publish-artifacts"

Write-Host "Applying P1 Set 032 from $repoRoot"

$files = @(
    "tools\build\publish-cloud-artifacts.ps1",
    "tools\build\publish-cloud-artifacts.cmd",
    "docs\azure\CLOUD_ARTIFACT_PUBLISHING.md",
    "docs\cloud-roadmap-cleanup\P1_SET_032_CLOUD_ARTIFACT_PUBLISHING.md"
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
Write-Host "P1 Set 032 applied."
Write-Host "Validation:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\build\publish-cloud-artifacts.ps1 -Clean"
