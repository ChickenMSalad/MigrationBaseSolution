$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p1-set024-app-service-deployment-scaffold"

Write-Host "Applying P1 Set 024 from $repoRoot"

$files = @(
    "deploy\azure\app-service\main.bicep",
    "deploy\azure\app-service\deploy-app-service.ps1",
    "deploy\azure\app-service\README.md",
    "docs\azure\APP_SERVICE_DEPLOYMENT_SCAFFOLD.md",
    "docs\cloud-roadmap-cleanup\P1_SET_024_APP_SERVICE_DEPLOYMENT_SCAFFOLD.md"
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
Write-Host "P1 Set 024 applied."
Write-Host "Optional Azure what-if validation requires Azure CLI:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\deploy\azure\app-service\deploy-app-service.ps1 -ResourceGroupName <resource-group> -WhatIf"
