Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-ScriptDirectory {
    if (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) {
        return $PSScriptRoot
    }

    $invocationPath = $MyInvocation.MyCommand.Path
    if (-not [string]::IsNullOrWhiteSpace($invocationPath)) {
        return Split-Path -Parent $invocationPath
    }

    return (Get-Location).Path
}

function Assert-FileExists {
    param([Parameter(Mandatory = $true)][string] $Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Missing expected file: $Path"
    }
}

$scriptDirectory = Get-ScriptDirectory
$repoRoot = Split-Path -Parent $scriptDirectory

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\AzureDeploymentReadinessSeverity.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\AzureDeploymentReadinessCheck.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\AzureDeploymentReadinessFinding.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\AzureDeploymentReadinessReport.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\AzureDeploymentReadinessChecklist.cs',
    'config\azure-runtime\deployment-readiness\deployment-readiness.sample.json'
)

$missing = @()
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        $missing += $relativePath
    }
}

if (@($missing).Length -gt 0) {
    Write-Host 'Missing expected P5.1.11 files:' -ForegroundColor Red
    foreach ($item in $missing) {
        Write-Host " - $item" -ForegroundColor Red
    }
    exit 1
}

$coreProject = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
Assert-FileExists -Path $coreProject

$projectFiles = @(Get-ChildItem -LiteralPath $repoRoot -Recurse -Filter '*.csproj' -File |
    Where-Object {
        $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
    })

$inlineVersionMatches = @()
foreach ($projectFile in $projectFiles) {
    $content = Get-Content -LiteralPath $projectFile.FullName -Raw
    if ($content -match '<PackageReference\b[^>]*\bVersion\s*=') {
        $inlineVersionMatches += $projectFile.FullName
    }
}

if (@($inlineVersionMatches).Length -gt 0) {
    Write-Host 'Inline PackageReference Version attributes detected:' -ForegroundColor Red
    foreach ($projectFile in $inlineVersionMatches) {
        Write-Host " - $projectFile" -ForegroundColor Red
    }
    exit 1
}

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $coreProject
if ($LASTEXITCODE -ne 0) {
    throw 'MigrationBase.Core restore failed.'
}

Write-Host 'Building MigrationBase.Core...'
dotnet build $coreProject --no-restore
if ($LASTEXITCODE -ne 0) {
    throw 'MigrationBase.Core build failed.'
}

Write-Host 'P5.1.11 Azure deployment readiness validation passed.' -ForegroundColor Green
