Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptPath = $MyInvocation.MyCommand.Path
    if ([string]::IsNullOrWhiteSpace($scriptPath)) { throw 'Unable to determine script path.' }
    $scriptDirectory = Split-Path -Parent $scriptPath
}
$repoRoot = Split-Path -Parent $scriptDirectory

function Join-RepoPath([string]$RelativePath) {
    return Join-Path $repoRoot $RelativePath
}

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Poison\AzureWorkerPoisonWorkAction.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Poison\AzureWorkerPoisonWorkClassification.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Poison\AzureWorkerPoisonWorkDisposition.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Poison\AzureWorkerPoisonWorkPolicy.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Poison\IAzureWorkerPoisonWorkClassifier.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Poison\AzureWorkerPoisonWorkClassifier.cs',
    'config\azure-runtime\workers\poison-work.policy.sample.json'
)

$missing = @()
foreach ($file in $expectedFiles) {
    $path = Join-RepoPath $file
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { $missing += $file }
}
if (@($missing).Length -gt 0) {
    Write-Host 'Missing expected P5.2.6 files:' -ForegroundColor Red
    foreach ($file in $missing) { Write-Host " - $file" -ForegroundColor Red }
    exit 1
}

$projectPath = Join-RepoPath 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) {
    throw "Missing project file: $projectPath"
}

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $projectPath
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build $projectPath --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'P5.2.6 Azure worker poison work contract validation passed.' -ForegroundColor Green
