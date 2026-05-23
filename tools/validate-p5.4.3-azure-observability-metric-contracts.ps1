Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptPath = $MyInvocation.MyCommand.Path
    if ([string]::IsNullOrWhiteSpace($scriptPath)) { throw 'Unable to resolve script path.' }
    $scriptDirectory = Split-Path -Parent $scriptPath
}
$repoRoot = Resolve-Path (Join-Path $scriptDirectory '..')

function Test-PathRequired {
    param([Parameter(Mandatory=$true)][string]$RelativePath)
    $fullPath = Join-Path $repoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $fullPath)) { return $RelativePath }
    return $null
}

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Observability\AzureMetricDescriptor.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Observability\AzureMetricKind.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Observability\AzureMetricRegistry.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Observability\IAzureMetricRegistry.cs',
    'config\azure-runtime\observability\metrics.registry.sample.json'
)

$missing = @()
foreach ($file in $expectedFiles) {
    $missingFile = Test-PathRequired -RelativePath $file
    if ($null -ne $missingFile) { $missing += $missingFile }
}

if (@($missing).Count -gt 0) {
    Write-Host 'Missing expected P5.4.3 files:' -ForegroundColor Red
    foreach ($file in $missing) { Write-Host " - $file" -ForegroundColor Red }
    exit 1
}

$projectPath = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (-not (Test-Path -LiteralPath $projectPath)) { throw "Missing project: ${projectPath}" }

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $projectPath
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build $projectPath --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'P5.4.3 Azure observability metric contract validation passed.' -ForegroundColor Green
