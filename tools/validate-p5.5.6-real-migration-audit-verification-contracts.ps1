Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDirectory = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $PSCommandPath }
$repoRoot = Split-Path -Parent $scriptDirectory

function Join-RepoPath([string]$RelativePath) {
    return Join-Path $repoRoot $RelativePath
}

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Validation\Audit\RealMigrationAuditVerificationContract.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Validation\Audit\RealMigrationAuditVerificationCheckpoint.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Validation\Audit\RealMigrationAuditEvidenceRequirement.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Validation\Audit\RealMigrationAuditVerificationLevel.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Validation\Audit\RealMigrationAuditVerificationResult.cs',
    'config\real-migration-validation\audit\audit-verification.sample.json'
)

$missing = @()
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-RepoPath $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        $missing += $relativePath
    }
}

if (@($missing).Length -gt 0) {
    Write-Host 'Missing expected P5.5.6 files:'
    foreach ($item in $missing) { Write-Host " - $item" }
    exit 1
}

$projectPath = Join-RepoPath 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "Missing project file: ${projectPath}"
}

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $projectPath
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build $projectPath --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'P5.5.6 real migration audit verification contract validation passed.'
