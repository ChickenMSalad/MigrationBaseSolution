$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set053-completion-checkpoint"

Write-Host "Applying P2 Set 053 from $repoRoot"

$files = @(
    "docs\cloud-roadmap-cleanup\P2_COMPLETION_CHECKPOINT.md",
    "docs\cloud-roadmap-cleanup\P3_RECOMMENDED_PLAN.md",
    "tools\test\validate-p2-completion.ps1",
    "tools\test\validate-p2-completion.cmd",
    "docs\cloud-roadmap-cleanup\P2_SET_053_COMPLETION_CHECKPOINT.md"
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
Write-Host "P2 Set 053 applied."
Write-Host "No build required."
Write-Host "After starting Admin API, validate with:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\test\validate-p2-completion.ps1 -BaseUrl http://localhost:5173 -AllowUnconfigured"
Write-Host ""
Write-Host "After this set is committed, P2 is complete."
