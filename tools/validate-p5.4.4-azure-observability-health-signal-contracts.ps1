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
    'src/Core/MigrationBase.Core/Cloud/Azure/Observability/AzureHealthSignalSeverity.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Observability/AzureHealthSignalStatus.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Observability/AzureHealthSignalDescriptor.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Observability/AzureHealthSignalSnapshot.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Observability/IAzureHealthSignalRegistry.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Observability/AzureHealthSignalRegistry.cs',
    'config/azure-runtime/observability/health-signals.sample.json'
)

$missing = @()
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) { $missing += $relativePath }
}

if (@($missing).Length -gt 0) {
    Write-Host 'Missing expected P5.4.4 files:'
    foreach ($item in $missing) { Write-Host " - $item" }
    throw 'P5.4.4 validation failed: missing expected files.'
}

$projectPath = Join-Path $repoRoot 'src/Core/MigrationBase.Core/MigrationBase.Core.csproj'
if (-not (Test-Path -LiteralPath $projectPath)) { throw "Missing project: ${projectPath}" }

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $projectPath
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build $projectPath --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'P5.4.4 Azure observability health signal contract validation passed.'
