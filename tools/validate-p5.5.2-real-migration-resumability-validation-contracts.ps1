Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptDirectory = Split-Path -Parent $PSCommandPath
}
$repoRoot = Split-Path -Parent $scriptDirectory

function Assert-FileExists {
    param([Parameter(Mandatory=$true)][string]$RelativePath)
    $fullPath = Join-Path $repoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Missing expected file: $RelativePath"
    }
}

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Validation\Resumability\RealMigrationResumabilityValidationContract.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Validation\Resumability\RealMigrationResumabilityEvidence.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Validation\Resumability\RealMigrationResumabilityValidationResult.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Validation\Resumability\IRealMigrationResumabilityValidator.cs',
    'config\azure-runtime\validation\resumability-validation.sample.json'
)

foreach ($file in $expectedFiles) { Assert-FileExists -RelativePath $file }

$projectPath = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) {
    throw "Missing MigrationBase.Core project at ${projectPath}"
}

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $projectPath
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build $projectPath --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'P5.5.2 real migration resumability validation contract validation passed.'
