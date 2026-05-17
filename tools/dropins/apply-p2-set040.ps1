$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set040-telemetry-checkpoint"

Write-Host "Applying P2 Set 040 from $repoRoot"

$files = @(
    "docs\cloud-roadmap-cleanup\P2_TELEMETRY_CHECKPOINT.md",
    "tools\test\validate-telemetry-stack.ps1",
    "tools\test\validate-telemetry-stack.cmd",
    "docs\cloud-roadmap-cleanup\P2_SET_040_TELEMETRY_CHECKPOINT.md"
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
Write-Host "P2 Set 040 applied."
Write-Host "No build is required."
Write-Host "After starting Admin API, validate with:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\test\validate-telemetry-stack.ps1 -BaseUrl http://localhost:5173"
