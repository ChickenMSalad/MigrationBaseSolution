$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p1-set036-cloud-platform-index"

Write-Host "Applying P1 Set 036 from $repoRoot"

$files = @(
    "docs\azure\CLOUD_PLATFORM_INDEX.md",
    "docs\cloud-roadmap-cleanup\P1_PHASE_SUMMARY.md",
    "docs\cloud-roadmap-cleanup\P1_SET_036_CLOUD_PLATFORM_INDEX.md"
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
Write-Host "P1 Set 036 applied."
Write-Host "No build is required."
