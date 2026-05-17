param(
    [string]$ResourceGroupName,
    [string]$Location = "eastus",
    [string]$EnvironmentName = "dev",
    [string]$NamePrefix = "migration",
    [string]$QueueExecutorImage = "migration-queue-executor:local",
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ResourceGroupName)) {
    throw "ResourceGroupName is required."
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\..")
$templatePath = Join-Path $repoRoot "deploy\azure\container-apps-job\main.bicep"

if (!(Test-Path $templatePath)) {
    throw "Bicep template not found: $templatePath"
}

Write-Host "Azure Container Apps Job deployment scaffold"
Write-Host "Resource group : $ResourceGroupName"
Write-Host "Location       : $Location"
Write-Host "Environment    : $EnvironmentName"
Write-Host "Name prefix    : $NamePrefix"
Write-Host "Image          : $QueueExecutorImage"
Write-Host "Template       : $templatePath"
Write-Host ""

if ($WhatIf) {
    az deployment group what-if `
        --resource-group $ResourceGroupName `
        --template-file $templatePath `
        --parameters location=$Location environmentName=$EnvironmentName namePrefix=$NamePrefix queueExecutorImage=$QueueExecutorImage
}
else {
    az deployment group create `
        --resource-group $ResourceGroupName `
        --template-file $templatePath `
        --parameters location=$Location environmentName=$EnvironmentName namePrefix=$NamePrefix queueExecutorImage=$QueueExecutorImage
}
