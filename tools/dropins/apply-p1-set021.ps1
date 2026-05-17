$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p1-set021-cloud-diagnostics-expansion"

Write-Host "Applying P1 Set 021 from $repoRoot"

$files = @(
    "tools\cloud\validate-cloud-diagnostics.ps1",
    "docs\cloud-roadmap-cleanup\P1_SET_021_CLOUD_DIAGNOSTICS_EXPANSION.md"
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
Write-Host "P1 Set 021 applied."
Write-Host "Template/docs validation:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\cloud\validate-cloud-diagnostics.ps1 -SkipHttp"
Write-Host ""
Write-Host "Full validation after starting Admin API:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\cloud\validate-cloud-diagnostics.ps1 -BaseUrl http://localhost:5173"
