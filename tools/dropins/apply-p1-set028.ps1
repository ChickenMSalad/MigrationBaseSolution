$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p1-set028-azure-storage-scaffold"

Write-Host "Applying P1 Set 028 from $repoRoot"

$files = @(
    "deploy\azure\storage\main.bicep",
    "deploy\azure\storage\deploy-storage.ps1",
    "deploy\azure\storage\README.md",
    "docs\azure\STORAGE_INFRASTRUCTURE_PLAN.md",
    "docs\cloud-roadmap-cleanup\P1_SET_028_AZURE_STORAGE_SCAFFOLD.md"
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
Write-Host "P1 Set 028 applied."
Write-Host "Optional Azure what-if validation requires Azure CLI:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\deploy\azure\storage\deploy-storage.ps1 -ResourceGroupName migration-dev-rg -WhatIf"
