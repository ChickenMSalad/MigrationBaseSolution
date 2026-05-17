$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set051-full-p2-validation"

Write-Host "Applying P2 Set 051 from $repoRoot"

$files = @(
    "tools\test\validate-full-p2-stack.ps1",
    "tools\test\validate-full-p2-stack.cmd",
    "docs\cloud-roadmap-cleanup\P2_SET_051_FULL_VALIDATION_AGGREGATOR.md"
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
Write-Host "P2 Set 051 applied."
Write-Host "No build required."
