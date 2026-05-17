$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p1-set035-release-validation"

Write-Host "Applying P1 Set 035 from $repoRoot"

$files = @(
    "tools\release\validate-release-readiness.ps1",
    "tools\release\validate-release-readiness.cmd",
    "docs\azure\RELEASE_READINESS_VALIDATION.md",
    "docs\cloud-roadmap-cleanup\P1_SET_035_RELEASE_READINESS_VALIDATION.md"
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
Write-Host "P1 Set 035 applied."
Write-Host "Validation:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\release\validate-release-readiness.ps1"
