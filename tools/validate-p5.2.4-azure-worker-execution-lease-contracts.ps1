Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptRoot = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $PSCommandPath }
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..')

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Leases\AzureWorkerExecutionLeaseDescriptor.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Leases\AzureWorkerExecutionLeaseState.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Leases\AzureWorkerExecutionLeaseStatus.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Leases\AzureWorkerExecutionLeasePolicy.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Leases\IAzureWorkerExecutionLeaseRegistry.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Leases\AzureWorkerExecutionLeaseRegistry.cs',
    'config\azure-runtime\workers\execution-leases.sample.json'
)

$missing = @()
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        $missing += $relativePath
    }
}

if (@($missing).Count -gt 0) {
    Write-Host 'Missing expected P5.2.4 files:' -ForegroundColor Red
    foreach ($item in $missing) { Write-Host " - $item" -ForegroundColor Red }
    exit 1
}

$projectPath = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "Missing required project: ${projectPath}"
}

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $projectPath
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build $projectPath --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'P5.2.4 Azure worker execution lease contract validation passed.' -ForegroundColor Green
