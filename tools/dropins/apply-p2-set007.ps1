$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set007-artifact-bridge-validation"

Write-Host "Applying P2 Set 007 from $repoRoot"

$files = @(
    "tools\test\smoke-artifact-storage-bridge.ps1",
    "tools\test\smoke-artifact-storage-bridge.cmd",
    "docs\cloud-roadmap-cleanup\P2_SET_007_ARTIFACT_BRIDGE_VALIDATION.md"
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
Write-Host "P2 Set 007 applied."
Write-Host "After starting Admin API, validate with:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-artifact-storage-bridge.ps1 -BaseUrl http://localhost:5173"
