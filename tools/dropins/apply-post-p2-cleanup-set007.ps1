$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\post-p2-cleanup-set007-p3-sql-baseline"

Write-Host "Applying Post-P2 Cleanup Set 007 from $repoRoot"

$files = @(
    "docs\post-p2-cleanup\POST_P2_CLEANUP_SET_007_P3_SQL_BASELINE.md",
    "docs\p3-planning\P3_SQL_OPERATIONAL_MODEL.md",
    "docs\p3-planning\P3_SQL_SCHEMA_STARTING_POINT.md",
    "docs\p3-planning\P3_EXECUTION_BOUNDARIES.md"
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
Write-Host "Post-P2 Cleanup Set 007 applied."
Write-Host "No build required."
