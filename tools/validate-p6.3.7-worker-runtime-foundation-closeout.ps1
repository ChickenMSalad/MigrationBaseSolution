Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptPath = $MyInvocation.MyCommand.Path
    if ([string]::IsNullOrWhiteSpace($scriptPath)) { throw 'Unable to resolve script path.' }
    $scriptDirectory = Split-Path -Parent $scriptPath
}

$repoRoot = Split-Path -Parent $scriptDirectory

$expectedFiles = @(
    'src/Core/MigrationBase.Core/Cloud/Azure/Runtime/Workers/Closeout/AzureWorkerRuntimeFoundationCloseout.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Runtime/Workers/Closeout/AzureWorkerRuntimeFoundationReadiness.cs',
    'config/azure-runtime/workers/p6.3.worker-runtime-foundation-closeout.sample.json'
)

$missing = @()
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) { $missing += $relativePath }
}

if (@($missing).Length -gt 0) {
    Write-Host 'Missing expected P6.3.7 files:'
    foreach ($item in $missing) { Write-Host " - $item" }
    throw 'P6.3.7 validation failed.'
}

$coreProject = Join-Path $repoRoot 'src/Core/MigrationBase.Core/MigrationBase.Core.csproj'
if (-not (Test-Path -LiteralPath $coreProject)) { throw "Missing project: $coreProject" }

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $coreProject
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build $coreProject --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'P6.3.7 worker runtime foundation closeout validation passed.'
