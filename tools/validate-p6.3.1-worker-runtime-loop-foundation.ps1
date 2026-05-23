Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptDirectory = Split-Path -Parent $PSCommandPath
}
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptDirectory = (Get-Location).Path
}

$repoRoot = Resolve-Path (Join-Path $scriptDirectory '..')
$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Runtime\AzureWorkerRuntimeLoopOptions.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Runtime\AzureWorkerRuntimeLoopState.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Runtime\AzureWorkerRuntimeLoopResult.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Runtime\IAzureWorkerRuntimeStep.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Runtime\IAzureWorkerRuntimeLoop.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Runtime\AzureWorkerRuntimeLoop.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Runtime\AzureWorkerRuntimeLoopFactory.cs'
)

$missing = @()
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        $missing += $relativePath
    }
}

if (@($missing).Length -gt 0) {
    Write-Host 'Missing expected P6.3.1 files:' -ForegroundColor Red
    foreach ($item in $missing) { Write-Host " - $item" -ForegroundColor Red }
    throw 'P6.3.1 validation failed.'
}

$projectPath = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "Missing project file: ${projectPath}"
}

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $projectPath
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build $projectPath --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'P6.3.1 worker runtime loop foundation validation passed.' -ForegroundColor Green
