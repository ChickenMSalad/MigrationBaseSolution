Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptPath = $MyInvocation.MyCommand.Path
    if ([string]::IsNullOrWhiteSpace($scriptPath)) { $scriptDirectory = (Get-Location).Path }
    else { $scriptDirectory = Split-Path -Parent $scriptPath }
}

$repoRoot = Split-Path -Parent $scriptDirectory

function Test-ExpectedFile {
    param([Parameter(Mandatory=$true)][string]$RelativePath)
    $fullPath = Join-Path $repoRoot $RelativePath
    return [System.IO.File]::Exists($fullPath)
}

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Runtime\Composition\Handoff\AzureRuntimeCompositionHandoffItem.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Runtime\Composition\Handoff\AzureRuntimeCompositionHandoffManifest.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Runtime\Composition\Handoff\AzureRuntimeCompositionHandoffFactory.cs',
    'config\azure-runtime\composition\p6.1-composition-handoff.sample.json'
)

$missing = @()
foreach ($relativePath in $expectedFiles) {
    if (-not (Test-ExpectedFile -RelativePath $relativePath)) {
        $missing += $relativePath
    }
}

if (@($missing).Length -gt 0) {
    Write-Host 'Missing expected P6.1.5 files:'
    foreach ($item in $missing) { Write-Host " - $item" }
    throw 'P6.1.5 validation failed.'
}

$coreProject = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (-not [System.IO.File]::Exists($coreProject)) {
    throw "Missing MigrationBase.Core project at ${coreProject}"
}

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $coreProject
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build $coreProject --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'P6.1.5 runtime composition handoff closeout validation passed.'
