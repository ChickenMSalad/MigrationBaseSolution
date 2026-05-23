Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-ScriptDirectory {
    if ($PSScriptRoot -and $PSScriptRoot.Trim().Length -gt 0) { return $PSScriptRoot }
    $path = $MyInvocation.MyCommand.Path
    if ($path -and $path.Trim().Length -gt 0) { return Split-Path -Parent $path }
    return (Get-Location).Path
}

$scriptDirectory = Get-ScriptDirectory
$repoRoot = Split-Path -Parent $scriptDirectory

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Drift\AzureEnvironmentDriftSeverity.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Drift\AzureEnvironmentDriftStatus.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Drift\AzureEnvironmentDriftDescriptor.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Drift\AzureEnvironmentDriftReport.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Drift\IAzureEnvironmentDriftRegistry.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Drift\AzureEnvironmentDriftRegistry.cs',
    'config\azure-runtime\drift\environment-drift.sample.json'
)

$missing = @()
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) { $missing += $relativePath }
}

if (@($missing).Count -gt 0) {
    Write-Host 'Missing expected P5.1.17 files:'
    foreach ($item in $missing) { Write-Host " - $item" }
    throw 'P5.1.17 validation failed.'
}

$projectPath = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "MigrationBase.Core project file was not found at ${projectPath}."
}

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $projectPath
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build $projectPath --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'P5.1.17 Azure environment drift contract validation passed.'
