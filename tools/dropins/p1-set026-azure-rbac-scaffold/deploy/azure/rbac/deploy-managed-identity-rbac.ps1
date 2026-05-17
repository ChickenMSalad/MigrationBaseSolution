param(
    [string]$ResourceGroupName,
    [string]$AdminApiPrincipalId,
    [string]$QueueExecutorPrincipalId,
    [string]$StorageAccountResourceId,
    [string]$KeyVaultResourceId = "",
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ResourceGroupName)) { throw "ResourceGroupName is required." }
if ([string]::IsNullOrWhiteSpace($AdminApiPrincipalId)) { throw "AdminApiPrincipalId is required." }
if ([string]::IsNullOrWhiteSpace($QueueExecutorPrincipalId)) { throw "QueueExecutorPrincipalId is required." }
if ([string]::IsNullOrWhiteSpace($StorageAccountResourceId)) { throw "StorageAccountResourceId is required." }

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\..")
$templatePath = Join-Path $repoRoot "deploy\azure\rbac\managed-identity-rbac.bicep"

if (!(Test-Path $templatePath)) {
    throw "Bicep template not found: $templatePath"
}

Write-Host "Azure Managed Identity RBAC scaffold"
Write-Host "Resource group          : $ResourceGroupName"
Write-Host "Admin API principal    : $AdminApiPrincipalId"
Write-Host "Queue Executor principal: $QueueExecutorPrincipalId"
Write-Host "Storage account        : $StorageAccountResourceId"
Write-Host "Key Vault              : $KeyVaultResourceId"
Write-Host ""

$params = @(
    "adminApiPrincipalId=$AdminApiPrincipalId",
    "queueExecutorPrincipalId=$QueueExecutorPrincipalId",
    "storageAccountResourceId=$StorageAccountResourceId",
    "keyVaultResourceId=$KeyVaultResourceId"
)

if ($WhatIf) {
    az deployment group what-if `
        --resource-group $ResourceGroupName `
        --template-file $templatePath `
        --parameters $params
}
else {
    az deployment group create `
        --resource-group $ResourceGroupName `
        --template-file $templatePath `
        --parameters $params
}
