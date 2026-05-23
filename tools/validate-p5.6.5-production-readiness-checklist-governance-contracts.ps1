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

$repoRoot = Resolve-Path (Join-Path $scriptDirectory '..')

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Governance\Readiness\AzureProductionReadinessChecklist.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Governance\Readiness\AzureProductionReadinessChecklistItem.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Governance\Readiness\AzureProductionReadinessDomain.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Governance\Readiness\AzureProductionReadinessSeverity.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Governance\Readiness\AzureProductionReadinessDecision.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Governance\Readiness\AzureProductionReadinessDecisionStatus.cs',
    'config\azure-runtime\governance\production-readiness.checklist.sample.json'
)

$missing = @()
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        $missing += $relativePath
    }
}

if (@($missing).Length -gt 0) {
    Write-Host 'Missing expected P5.6.5 files:'
    foreach ($path in $missing) { Write-Host " - $path" }
    throw 'P5.6.5 validation failed: missing expected files.'
}

$projectPath = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "Missing required project file: ${projectPath}"
}

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $projectPath
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build $projectPath --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'P5.6.5 production readiness checklist governance validation passed.'
