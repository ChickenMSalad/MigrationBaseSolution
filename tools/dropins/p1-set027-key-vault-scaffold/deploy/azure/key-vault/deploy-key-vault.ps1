param(
    [string]$ResourceGroupName,
    [string]$Location = "eastus",
    [string]$EnvironmentName = "dev",
    [string]$NamePrefix = "migration",
    [switch]$EnablePurgeProtection,
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ResourceGroupName)) {
    throw "ResourceGroupName is required."
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\..")
$templatePath = Join-Path $repoRoot "deploy\azure\key-vault\main.bicep"

if (!(Test-Path $templatePath)) {
    throw "Bicep template not found: $templatePath"
}

Write-Host "Azure Key Vault deployment scaffold"
Write-Host "Resource group  : $ResourceGroupName"
Write-Host "Location        : $Location"
Write-Host "Environment     : $EnvironmentName"
Write-Host "Name prefix     : $NamePrefix"
Write-Host "Purge protection: $EnablePurgeProtection"
Write-Host ""

if ($WhatIf) {
    az deployment group what-if `
        --resource-group $ResourceGroupName `
        --template-file $templatePath `
        --parameters location=$Location environmentName=$EnvironmentName namePrefix=$NamePrefix enablePurgeProtection=$EnablePurgeProtection
}
else {
    az deployment group create `
        --resource-group $ResourceGroupName `
        --template-file $templatePath `
        --parameters location=$Location environmentName=$EnvironmentName namePrefix=$NamePrefix enablePurgeProtection=$EnablePurgeProtection
}
