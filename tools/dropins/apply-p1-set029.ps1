$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p1-set029-deployment-outputs-config"

Write-Host "Applying P1 Set 029 from $repoRoot"

$files = @(
    "tools\cloud\new-cloud-appsettings-from-outputs.ps1",
    "tools\cloud\new-cloud-appsettings-from-outputs.cmd",
    "docs\azure\DEPLOYMENT_OUTPUTS_TO_APPSETTINGS.md",
    "docs\cloud-roadmap-cleanup\P1_SET_029_DEPLOYMENT_OUTPUTS_CONFIG.md"
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
Write-Host "P1 Set 029 applied."
Write-Host "Optional validation:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\cloud\new-cloud-appsettings-from-outputs.ps1 -EnvironmentName dev -StorageAccountName migrationdevsa -ArtifactContainerName migration-artifacts-dev -ControlPlaneContainerName migration-control-plane-dev -RunQueueName migration-runs-dev"
