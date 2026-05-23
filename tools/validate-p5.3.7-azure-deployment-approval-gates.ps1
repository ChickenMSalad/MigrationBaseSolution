Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptPath = $MyInvocation.MyCommand.Path
    if ([string]::IsNullOrWhiteSpace($scriptPath)) { throw 'Unable to resolve script path.' }
    $scriptDirectory = Split-Path -Parent $scriptPath
}

$repoRoot = Split-Path -Parent $scriptDirectory

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\AzureDeploymentApprovalDecision.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\AzureDeploymentApprovalRequirement.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\AzureDeploymentApprovalGate.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\IAzureDeploymentApprovalGateRegistry.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\AzureDeploymentApprovalGateRegistry.cs',
    'config\azure-runtime\deployment\approval-gates.sample.json'
)

$missingFiles = @()
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        $missingFiles += $relativePath
    }
}

if (@($missingFiles).Length -gt 0) {
    Write-Host 'Missing expected P5.3.7 files:'
    foreach ($missingFile in $missingFiles) { Write-Host " - $missingFile" }
    exit 1
}

$projectPath = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "Missing expected project: ${projectPath}"
}

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $projectPath
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build $projectPath --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'P5.3.7 Azure deployment approval gate validation passed.'
