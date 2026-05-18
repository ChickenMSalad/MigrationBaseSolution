$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\post-p2-cleanup-set001-fix"

Write-Host "Applying Post-P2 Cleanup Set 001 fix from $repoRoot"

$source = Join-Path $payloadRoot "tools\maintenance\audit-p2-docs-tools.ps1"
$target = Join-Path $repoRoot "tools\maintenance\audit-p2-docs-tools.ps1"

if (!(Test-Path (Split-Path $target -Parent))) {
    New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force | Out-Null
}

Copy-Item $source $target -Force
Write-Host "Patched tools\maintenance\audit-p2-docs-tools.ps1"
Write-Host ""
Write-Host "Run:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\maintenance\audit-p2-docs-tools.ps1 -WriteCleanupCommands"
