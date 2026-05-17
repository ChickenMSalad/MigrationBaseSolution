$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p1-set026-azure-rbac-scaffold"

Write-Host "Applying P1 Set 026 from $repoRoot"

$files = @(
    "deploy\azure\rbac\managed-identity-rbac.bicep",
    "deploy\azure\rbac\deploy-managed-identity-rbac.ps1",
    "deploy\azure\rbac\README.md",
    "docs\azure\MANAGED_IDENTITY_RBAC_PLAN.md",
    "docs\cloud-roadmap-cleanup\P1_SET_026_AZURE_RBAC_SCAFFOLD.md"
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
Write-Host "P1 Set 026 applied."
Write-Host "Optional Azure what-if validation requires Azure CLI and real principal/resource ids."
