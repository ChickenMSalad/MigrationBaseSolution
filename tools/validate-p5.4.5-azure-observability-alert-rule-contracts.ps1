Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptPath = $PSCommandPath
    if ([string]::IsNullOrWhiteSpace($scriptPath)) { $scriptPath = $MyInvocation.MyCommand.Path }
    if ([string]::IsNullOrWhiteSpace($scriptPath)) { throw 'Unable to resolve script path.' }
    $scriptDirectory = Split-Path -Parent $scriptPath
}

$repoRoot = Split-Path -Parent $scriptDirectory

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Observability\Alerts\AzureAlertRuleDescriptor.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Observability\Alerts\AzureAlertSeverity.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Observability\Alerts\AzureAlertEvaluationWindow.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Observability\Alerts\AzureAlertThreshold.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Observability\Alerts\AzureAlertRuleCatalog.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Observability\Alerts\IAzureAlertRuleCatalog.cs',
    'config\azure-runtime\observability\alerts\alert-rules.sample.json'
)

$missing = @()
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        $missing += $relativePath
    }
}

if (@($missing).Length -gt 0) {
    Write-Host 'Missing expected P5.4.5 files:'
    foreach ($path in $missing) { Write-Host " - $path" }
    exit 1
}

$projectPath = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) {
    throw "Missing project file: ${projectPath}"
}

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $projectPath
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build $projectPath --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'P5.4.5 Azure observability alert rule contract validation passed.'
