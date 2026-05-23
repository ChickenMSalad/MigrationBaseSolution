Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-ScriptDirectory {
    if (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) { return $PSScriptRoot }
    if ($MyInvocation.MyCommand.Path) { return Split-Path -Parent $MyInvocation.MyCommand.Path }
    return (Get-Location).Path
}

$scriptDirectory = Get-ScriptDirectory
$repoRoot = Split-Path -Parent $scriptDirectory

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\ExecutionValidation\AzureRealMigrationValidationScenario.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\ExecutionValidation\AzureRealMigrationValidationCheckpoint.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\ExecutionValidation\AzureRealMigrationValidationResult.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\ExecutionValidation\IAzureRealMigrationValidationRegistry.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\ExecutionValidation\AzureRealMigrationValidationRegistry.cs',
    'config\azure-runtime\execution-validation\real-migration-validation.scenarios.sample.json'
)

$missing = @()
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        $missing += $relativePath
    }
}

if (@($missing).Length -gt 0) {
    Write-Host 'Missing expected P5.5.1 files:'
    foreach ($item in $missing) { Write-Host " - $item" }
    exit 1
}

$projectPath = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "Missing project file: $projectPath"
}

$projectFiles = @(Get-ChildItem -LiteralPath (Join-Path $repoRoot 'src') -Recurse -Filter '*.csproj' -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' })

$inlineVersionHits = @()
foreach ($project in $projectFiles) {
    $content = Get-Content -LiteralPath $project.FullName -Raw
    if ($content -match '<PackageReference\s+[^>]*\bVersion\s*=') {
        $inlineVersionHits += $project.FullName
    }
}

if (@($inlineVersionHits).Length -gt 0) {
    Write-Host 'Inline PackageReference Version attributes detected:'
    foreach ($hit in $inlineVersionHits) { Write-Host " - $hit" }
    exit 1
}

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $projectPath
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build $projectPath --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'P5.5.1 real migration execution validation contract validation passed.'
