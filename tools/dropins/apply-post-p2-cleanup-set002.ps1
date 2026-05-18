$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\post-p2-cleanup-set002-dropin-artifacts"

Write-Host "Applying Post-P2 Cleanup Set 002 from $repoRoot"

$files = @(
    "docs\post-p2-cleanup\POST_P2_CLEANUP_SET_002_DROPIN_ARTIFACTS.md",
    "tools\maintenance\remove-post-p2-dropin-artifacts.ps1"
)

foreach ($relative in $files) {
    $source = Join-Path $payloadRoot $relative
    $target = Join-Path $repoRoot $relative
    $targetDirectory = Split-Path $target -Parent

    if (!(Test-Path $source)) {
        throw "Missing file: $source"
    }

    if (!(Test-Path $targetDirectory)) {
        New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
    }

    Copy-Item $source $target -Force
    Write-Host "Verified $relative"
}

Write-Host ""
Write-Host "Post-P2 Cleanup Set 002 applied."
Write-Host "Preview cleanup with:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\maintenance\remove-post-p2-dropin-artifacts.ps1"
Write-Host ""
Write-Host "Generate reviewable git rm commands with:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\maintenance\remove-post-p2-dropin-artifacts.ps1 -WriteCommandFile"
