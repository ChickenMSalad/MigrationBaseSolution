Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptDirectory = Split-Path -Parent $PSCommandPath
}
$repoRoot = Split-Path -Parent $scriptDirectory

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Infrastructure\Storage\AzureStorageAccountBinding.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Infrastructure\Storage\AzureStorageContainerBinding.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Infrastructure\Storage\IAzureStorageBindingRegistry.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Infrastructure\Storage\AzureStorageBindingRegistry.cs',
    'config\azure-runtime\infrastructure\storage-bindings.sample.json'
)

$missing = @()
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        $missing += $relativePath
    }
}

if (@($missing).Count -gt 0) {
    Write-Host 'Missing expected P6.2.3 files:'
    foreach ($item in $missing) { Write-Host " - $item" }
    throw 'P6.2.3 validation failed.'
}

$projectPath = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "Missing project: ${projectPath}"
}

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $projectPath
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build $projectPath --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'P6.2.3 Azure storage binding boundary contract validation passed.'
