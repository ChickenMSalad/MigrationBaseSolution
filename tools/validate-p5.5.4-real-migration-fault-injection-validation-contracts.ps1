Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $PSCommandPath }
$repoRoot = Split-Path -Parent $scriptRoot

$expectedFiles = @(
    'src/Core/MigrationBase.Core/Cloud/Azure/ExecutionValidation/RealMigrationFaultInjectionScenario.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/ExecutionValidation/RealMigrationFaultInjectionPlan.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/ExecutionValidation/RealMigrationFaultInjectionResult.cs',
    'config/real-migration-validation/fault-injection/fault-injection.scenarios.sample.json'
)

$missing = @()
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        $missing += $relativePath
    }
}

if (@($missing).Count -gt 0) {
    Write-Host 'Missing expected P5.5.4 files:'
    foreach ($item in $missing) { Write-Host " - $item" }
    throw 'P5.5.4 validation failed.'
}

$coreProject = Join-Path $repoRoot 'src/Core/MigrationBase.Core/MigrationBase.Core.csproj'
if (-not (Test-Path -LiteralPath $coreProject)) {
    throw 'MigrationBase.Core project file was not found.'
}

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $coreProject
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build $coreProject --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'P5.5.4 real migration fault-injection validation contract validation passed.'
