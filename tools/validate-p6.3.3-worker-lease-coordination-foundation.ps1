Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptPath = $MyInvocation.MyCommand.Path
    if ([string]::IsNullOrWhiteSpace($scriptPath)) { $scriptRoot = (Get-Location).Path }
    else { $scriptRoot = Split-Path -Parent $scriptPath }
}

$repoRoot = Split-Path -Parent $scriptRoot
$coreProject = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Runtime\Workers\Leasing\AzureWorkerLeaseAcquisitionRequest.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Runtime\Workers\Leasing\AzureWorkerLeaseAcquisitionResult.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Runtime\Workers\Leasing\AzureWorkerLeaseRenewalRequest.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Runtime\Workers\Leasing\AzureWorkerLeaseRenewalResult.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Runtime\Workers\Leasing\IAzureWorkerLeaseCoordinator.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Runtime\Workers\Leasing\NoOpAzureWorkerLeaseCoordinator.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Runtime\Workers\Leasing\AzureWorkerLeasePolicy.cs',
    'config\azure-runtime\worker-runtime\worker-lease-policy.sample.json'
)

$missing = @()
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) { $missing += $relativePath }
}

if ($missing.Length -gt 0) {
    Write-Host 'Missing expected P6.3.3 files:' -ForegroundColor Red
    foreach ($item in $missing) { Write-Host " - $item" -ForegroundColor Red }
    throw 'P6.3.3 validation failed.'
}

if (-not (Test-Path -LiteralPath $coreProject)) {
    throw "Missing project file: $coreProject"
}

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $coreProject --nologo
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build $coreProject --no-restore --nologo
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'P6.3.3 worker lease coordination foundation validation passed.' -ForegroundColor Green
