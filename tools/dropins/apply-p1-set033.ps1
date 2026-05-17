$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p1-set033-ci-publish-artifacts"

Write-Host "Applying P1 Set 033 from $repoRoot"

$files = @(
    ".github\workflows\migration-platform-validation.yml",
    "docs\azure\CI_PUBLISH_ARTIFACTS.md",
    "docs\cloud-roadmap-cleanup\P1_SET_033_CI_PUBLISH_ARTIFACTS.md"
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
Write-Host "P1 Set 033 applied."
Write-Host "No local build is required, but you can validate publish locally:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\build\publish-cloud-artifacts.ps1 -Clean"
