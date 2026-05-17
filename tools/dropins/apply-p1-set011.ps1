$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p1-set011-cloud-diagnostics-validation"

Write-Host "Applying P1 Set 011 from $repoRoot"

$files = @(
    "tools\cloud\validate-cloud-diagnostics.ps1",
    "tools\cloud\validate-cloud-diagnostics.cmd",
    "docs\cloud-roadmap-cleanup\P1_SET_011_CLOUD_DIAGNOSTICS_VALIDATION.md"
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
Write-Host "P1 Set 011 applied."
Write-Host "Template-only validation:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\cloud\validate-cloud-diagnostics.ps1 -SkipHttp"
Write-Host ""
Write-Host "Full validation after starting Admin API:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\cloud\validate-cloud-diagnostics.ps1 -BaseUrl http://localhost:5173"
