Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptPath = $MyInvocation.MyCommand.Path
    if ([string]::IsNullOrWhiteSpace($scriptPath)) {
        $scriptDirectory = (Get-Location).Path
    }
    else {
        $scriptDirectory = Split-Path -Parent $scriptPath
    }
}

$repoRoot = Split-Path -Parent $scriptDirectory

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Runtime\Composition\Binding\AzureRuntimeCompositionBinding.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Runtime\Composition\Binding\AzureRuntimeCompositionBindingKind.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Runtime\Composition\Binding\AzureRuntimeCompositionBindingRequirement.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Runtime\Composition\Binding\AzureRuntimeCompositionBindingRegistry.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Runtime\Composition\Binding\AzureRuntimeCompositionBindingValidationResult.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Runtime\Composition\Binding\IAzureRuntimeCompositionBindingRegistry.cs',
    'config\azure-runtime\runtime-composition\host-bindings.sample.json'
)

$missingFiles = @()
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        $missingFiles += $relativePath
    }
}

if (@($missingFiles).Length -gt 0) {
    Write-Host 'Missing expected P6.1.4 files:'
    foreach ($missingFile in $missingFiles) {
        Write-Host " - $missingFile"
    }
    throw 'P6.1.4 validation failed.'
}

$coreProject = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (-not (Test-Path -LiteralPath $coreProject -PathType Leaf)) {
    throw "Missing project file: $coreProject"
}

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $coreProject
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build $coreProject --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'P6.1.4 runtime composition host binding contract validation passed.'
