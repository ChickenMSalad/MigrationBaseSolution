$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p1-set013-promotion-checklist"

Write-Host "Applying P1 Set 013 from $repoRoot"

$files = @(
    "docs\azure\AZURE_ENVIRONMENT_PROMOTION_CHECKLIST.md",
    "tools\cloud\show-promotion-checklist.ps1",
    "tools\cloud\show-promotion-checklist.cmd",
    "docs\cloud-roadmap-cleanup\P1_SET_013_ENVIRONMENT_PROMOTION_CHECKLIST.md"
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
Write-Host "P1 Set 013 applied."
Write-Host "Optional validation:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\cloud\show-promotion-checklist.ps1 -Environment dev"
