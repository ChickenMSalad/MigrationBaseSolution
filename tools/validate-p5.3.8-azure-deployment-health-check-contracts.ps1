Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptPath = $PSCommandPath
    if ([string]::IsNullOrWhiteSpace($scriptPath)) { $scriptPath = $MyInvocation.MyCommand.Path }
    if ([string]::IsNullOrWhiteSpace($scriptPath)) { throw 'Unable to resolve script path.' }
    $scriptDirectory = Split-Path -Parent $scriptPath
}
$repoRoot = Split-Path -Parent $scriptDirectory

$expectedFiles = @(
    'src/Core/MigrationBase.Core/Cloud/Azure/Deployment/HealthChecks/AzureDeploymentHealthCheckDescriptor.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Deployment/HealthChecks/AzureDeploymentHealthCheckScope.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Deployment/HealthChecks/AzureDeploymentHealthCheckSeverity.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Deployment/HealthChecks/AzureDeploymentHealthCheckResult.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Deployment/HealthChecks/AzureDeploymentHealthCheckStatus.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Deployment/HealthChecks/AzureDeploymentHealthCheckRegistry.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Deployment/HealthChecks/IAzureDeploymentHealthCheckRegistry.cs',
    'config/azure-runtime/deployment/health-checks/health-checks.sample.json'
)

$missing = @()
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) { $missing += $relativePath }
}
if (@($missing).Length -gt 0) {
    Write-Host 'Missing expected P5.3.8 files:'
    foreach ($item in $missing) { Write-Host " - $item" }
    throw 'P5.3.8 validation failed.'
}

$projectPath = Join-Path $repoRoot 'src/Core/MigrationBase.Core/MigrationBase.Core.csproj'
if (Test-Path -LiteralPath $projectPath -PathType Leaf) {
    Write-Host 'Restoring MigrationBase.Core...'
    dotnet restore $projectPath
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

    Write-Host 'Building MigrationBase.Core...'
    dotnet build $projectPath --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }
}
else {
    Write-Host 'MigrationBase.Core.csproj not found; skipping project build check.'
}

Write-Host 'P5.3.8 Azure deployment health-check contract validation passed.'
