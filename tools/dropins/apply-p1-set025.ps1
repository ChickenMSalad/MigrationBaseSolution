$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p1-set025-worker-deployment-scaffold"

Write-Host "Applying P1 Set 025 from $repoRoot"

$files = @(
    "deploy\azure\container-apps-job\main.bicep",
    "deploy\azure\container-apps-job\deploy-container-apps-job.ps1",
    "deploy\azure\container-apps-job\README.md",
    "docs\azure\QUEUE_EXECUTOR_CONTAINER_APPS_JOB.md",
    "docs\cloud-roadmap-cleanup\P1_SET_025_QUEUE_EXECUTOR_DEPLOYMENT_SCAFFOLD.md"
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
Write-Host "P1 Set 025 applied."
Write-Host "Optional Azure what-if validation requires Azure CLI:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\deploy\azure\container-apps-job\deploy-container-apps-job.ps1 -ResourceGroupName <resource-group> -QueueExecutorImage <registry>/migration-queue-executor:dev -WhatIf"
