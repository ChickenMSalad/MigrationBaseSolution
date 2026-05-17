$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p1-set027-key-vault-scaffold"

Write-Host "Applying P1 Set 027 from $repoRoot"

$files = @(
    "deploy\azure\key-vault\main.bicep",
    "deploy\azure\key-vault\deploy-key-vault.ps1",
    "deploy\azure\key-vault\README.md",
    "docs\azure\KEY_VAULT_SECRET_NAMING.md",
    "docs\cloud-roadmap-cleanup\P1_SET_027_KEY_VAULT_SCAFFOLD.md"
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
Write-Host "P1 Set 027 applied."
Write-Host "Optional Azure what-if validation requires Azure CLI:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\deploy\azure\key-vault\deploy-key-vault.ps1 -ResourceGroupName <resource-group> -WhatIf"
