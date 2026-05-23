Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptPath = $MyInvocation.MyCommand.Path
    if ([string]::IsNullOrWhiteSpace($scriptPath)) { $scriptPath = $PSCommandPath }
    if ([string]::IsNullOrWhiteSpace($scriptPath)) { throw 'Unable to resolve script path.' }
    $scriptDirectory = Split-Path -Parent $scriptPath
}

$repoRoot = Split-Path -Parent $scriptDirectory
$coreProject = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Runtime\Composition\AzureRuntimeCompositionPlan.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Runtime\Composition\AzureRuntimeCompositionPlanner.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Runtime\Composition\AzureRuntimeCompositionStep.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Runtime\Composition\IAzureRuntimeCompositionPlanner.cs'
)

foreach ($relativePath in $expectedFiles) {
    $path = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Missing expected P6.1.2 file: $relativePath"
    }
}

if (-not (Test-Path -LiteralPath $coreProject -PathType Leaf)) {
    throw "Missing project file: $coreProject"
}

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $coreProject
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build $coreProject --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'P6.1.2 runtime composition plan builder validation passed.'
