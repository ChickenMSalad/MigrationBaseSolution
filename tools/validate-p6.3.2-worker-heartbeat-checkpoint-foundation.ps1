Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptPath = $MyInvocation.MyCommand.Path
    if ([string]::IsNullOrWhiteSpace($scriptPath)) { $scriptPath = $PSCommandPath }
    if ([string]::IsNullOrWhiteSpace($scriptPath)) { throw 'Unable to resolve script path.' }
    $scriptDirectory = Split-Path -Parent $scriptPath
}

$repoRoot = Split-Path -Parent $scriptDirectory

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Runtime\Worker\AzureWorkerHeartbeatCheckpointOptions.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Runtime\Worker\AzureWorkerHeartbeatCheckpointResult.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Runtime\Worker\AzureWorkerHeartbeatCheckpointStatus.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Runtime\Worker\AzureWorkerHeartbeatCheckpointValidator.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Runtime\Worker\AzureWorkerRuntimeHeartbeatCheckpoint.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Runtime\Worker\IAzureWorkerHeartbeatCheckpointRecorder.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Runtime\Worker\NoopAzureWorkerHeartbeatCheckpointRecorder.cs',
    'config\azure-runtime\worker\heartbeat-checkpoint.sample.json'
)

$missingFiles = @()
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        $missingFiles += $relativePath
    }
}

if (@($missingFiles).Count -gt 0) {
    Write-Host 'Missing expected P6.3.2 files:'
    foreach ($missingFile in $missingFiles) { Write-Host " - $missingFile" }
    throw 'P6.3.2 validation failed.'
}

$coreProject = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (-not (Test-Path -LiteralPath $coreProject -PathType Leaf)) {
    throw "Missing MigrationBase.Core project: $coreProject"
}

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $coreProject
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build $coreProject --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'P6.3.2 worker heartbeat checkpoint foundation validation passed.'
