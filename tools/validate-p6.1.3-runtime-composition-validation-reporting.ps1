Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $commandPath = $MyInvocation.MyCommand.Path
    if (-not [string]::IsNullOrWhiteSpace($commandPath)) {
        $scriptDirectory = Split-Path -Parent $commandPath
    }
}
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    throw 'Unable to resolve script directory.'
}

$repoRoot = Split-Path -Parent $scriptDirectory
$coreProject = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Runtime\Composition\AzureRuntimeCompositionValidationFinding.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Runtime\Composition\AzureRuntimeCompositionValidationReport.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Runtime\Composition\AzureRuntimeCompositionValidationReportBuilder.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Runtime\Composition\IAzureRuntimeCompositionValidationReporter.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Runtime\Composition\AzureRuntimeCompositionValidationReporter.cs',
    'config\azure-runtime\composition\runtime-composition-validation.sample.json'
)

$missing = @()
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        $missing += $relativePath
    }
}

if (@($missing).Length -gt 0) {
    Write-Host 'Missing expected P6.1.3 files:'
    foreach ($item in @($missing)) {
        Write-Host " - $item"
    }
    throw 'P6.1.3 validation failed.'
}

if (-not (Test-Path -LiteralPath $coreProject)) {
    throw "Missing MigrationBase.Core project: ${coreProject}"
}

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $coreProject
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build $coreProject --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'P6.1.3 runtime composition validation reporting validation passed.'
