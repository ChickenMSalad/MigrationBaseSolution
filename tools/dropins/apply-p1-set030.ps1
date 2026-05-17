$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p1-set030-azure-deployment-orchestration"

Write-Host "Applying P1 Set 030 from $repoRoot"

$files = @(
    "deploy\azure\deploy-cloud-scaffold.ps1",
    "deploy\azure\deploy-cloud-scaffold.cmd",
    "deploy\azure\README.md",
    "docs\azure\AZURE_DEPLOYMENT_ORCHESTRATION.md",
    "docs\cloud-roadmap-cleanup\P1_SET_030_AZURE_DEPLOYMENT_ORCHESTRATION.md"
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
Write-Host "P1 Set 030 applied."
Write-Host "Optional Azure what-if validation requires Azure CLI:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\deploy\azure\deploy-cloud-scaffold.ps1 -ResourceGroupName migration-dev-rg -WhatIf"
