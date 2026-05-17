param(
    [string]$ResourceGroupName,
    [string]$Location = "eastus",
    [string]$EnvironmentName = "dev",
    [string]$NamePrefix = "migration",
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ResourceGroupName)) {
    throw "ResourceGroupName is required."
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\..")
$templatePath = Join-Path $repoRoot "deploy\azure\app-service\main.bicep"

if (!(Test-Path $templatePath)) {
    throw "Bicep template not found: $templatePath"
}

Write-Host "Azure App Service deployment scaffold"
Write-Host "Resource group : $ResourceGroupName"
Write-Host "Location       : $Location"
Write-Host "Environment    : $EnvironmentName"
Write-Host "Name prefix    : $NamePrefix"
Write-Host "Template       : $templatePath"
Write-Host ""

if ($WhatIf) {
    az deployment group what-if `
        --resource-group $ResourceGroupName `
        --template-file $templatePath `
        --parameters location=$Location environmentName=$EnvironmentName namePrefix=$NamePrefix
}
else {
    az deployment group create `
        --resource-group $ResourceGroupName `
        --template-file $templatePath `
        --parameters location=$Location environmentName=$EnvironmentName namePrefix=$NamePrefix
}
