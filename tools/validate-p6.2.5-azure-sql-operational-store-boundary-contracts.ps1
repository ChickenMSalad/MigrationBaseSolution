Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptPath = $MyInvocation.MyCommand.Path
    if ([string]::IsNullOrWhiteSpace($scriptPath)) { $scriptPath = $PSCommandPath }
    if ([string]::IsNullOrWhiteSpace($scriptPath)) { throw 'Unable to resolve script path.' }
    $scriptDirectory = Split-Path -Parent $scriptPath
}

$repoRoot = Resolve-Path (Join-Path $scriptDirectory '..')
$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Integration\Sql\AzureSqlOperationalStoreDescriptor.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Integration\Sql\AzureSqlOperationalStoreRegistry.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Integration\Sql\IAzureSqlOperationalStoreRegistry.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Integration\Sql\AzureSqlOperationalStoreValidationResult.cs',
    'config\azure-runtime\integration\sql-operational-stores.sample.json'
)

$missing = @()
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) { $missing += $relativePath }
}

if (@($missing).Length -gt 0) {
    Write-Host 'Missing expected P6.2.5 files:'
    foreach ($item in $missing) { Write-Host " - $item" }
    throw 'P6.2.5 validation failed.'
}

$projectPath = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (-not (Test-Path -LiteralPath $projectPath)) { throw "Missing project: ${projectPath}" }

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $projectPath
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build $projectPath --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'P6.2.5 Azure SQL operational store boundary contract validation passed.'
