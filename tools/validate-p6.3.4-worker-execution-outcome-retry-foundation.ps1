Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptPath = $PSCommandPath
    if ([string]::IsNullOrWhiteSpace($scriptPath)) { $scriptPath = $MyInvocation.MyCommand.Path }
    if ([string]::IsNullOrWhiteSpace($scriptPath)) { throw 'Unable to determine script path.' }
    $scriptDirectory = Split-Path -Parent $scriptPath
}

$repoRoot = (Resolve-Path (Join-Path $scriptDirectory '..')).Path

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Worker\Runtime\AzureWorkerExecutionOutcomeKind.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Worker\Runtime\AzureWorkerRetryDisposition.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Worker\Runtime\AzureWorkerExecutionOutcome.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Worker\Runtime\AzureWorkerRetryPolicy.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Worker\Runtime\IAzureWorkerExecutionOutcomeClassifier.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Worker\Runtime\AzureWorkerExecutionOutcomeClassifier.cs',
    'config\azure-runtime\worker\execution-outcome-retry.sample.json'
)

$missing = @()
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) { $missing += $relativePath }
}

if (@($missing).Length -gt 0) {
    Write-Host 'Missing expected P6.3.4 files:'
    foreach ($item in $missing) { Write-Host " - $item" }
    throw 'P6.3.4 validation failed.'
}

$projectPath = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (-not (Test-Path -LiteralPath $projectPath)) { throw "Missing project: ${projectPath}" }

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $projectPath
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build $projectPath --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'P6.3.4 worker execution outcome retry foundation validation passed.'
