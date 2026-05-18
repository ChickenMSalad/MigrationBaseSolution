$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\post-p2-cleanup-set009-fix"

Write-Host "Applying Post-P2 Cleanup Set 009 fix from $repoRoot"

$files = @(
    "tools\maintenance\validate-post-p2-cleanup.ps1",
    "docs\post-p2-cleanup\POST_P2_CLEANUP_SET_009_FIX.md"
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
    Write-Host "Patched $relative"
}

Write-Host ""
Write-Host "Post-P2 Cleanup Set 009 fix applied."
Write-Host "Run:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\maintenance\validate-post-p2-cleanup.ps1"
