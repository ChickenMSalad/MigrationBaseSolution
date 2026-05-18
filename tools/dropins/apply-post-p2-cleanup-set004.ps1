$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\post-p2-cleanup-set004-doc-inventory"

Write-Host "Applying Post-P2 Cleanup Set 004 from $repoRoot"

$files = @(
    "docs\post-p2-cleanup\POST_P2_CLEANUP_SET_004_DOC_INVENTORY.md",
    "tools\maintenance\audit-p2-docs.ps1"
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
Write-Host "Post-P2 Cleanup Set 004 applied."
Write-Host "Run:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\maintenance\audit-p2-docs.ps1"
