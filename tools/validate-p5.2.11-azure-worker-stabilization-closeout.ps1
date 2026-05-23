Set-StrictMode -Version Latest
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
$repoRootPath = $repoRoot.Path

function Join-RepoPath {
    param([Parameter(Mandatory = $true)][string]$RelativePath)
    return Join-Path $repoRootPath $RelativePath
}

function Assert-FileExists {
    param([Parameter(Mandatory = $true)][string]$RelativePath)
    $path = Join-RepoPath $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Missing expected P5.2.11 file: $RelativePath"
    }
}

function Assert-FileContains {
    param(
        [Parameter(Mandatory = $true)][string]$RelativePath,
        [Parameter(Mandatory = $true)][string]$Needle
    )
    $path = Join-RepoPath $RelativePath
    $content = Get-Content -LiteralPath $path -Raw
    if ($content.IndexOf($Needle, [System.StringComparison]::Ordinal) -lt 0) {
        throw "Expected file $RelativePath to contain marker: $Needle"
    }
}

$expectedFiles = @(
    'src/Core/MigrationBase.Core/Cloud/Azure/Workers/Stabilization/AzureWorkerStabilizationReadinessStatus.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Workers/Stabilization/AzureWorkerStabilizationChecklistItem.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Workers/Stabilization/AzureWorkerStabilizationReadinessReport.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Workers/Stabilization/IAzureWorkerStabilizationReadinessRegistry.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Workers/Stabilization/AzureWorkerStabilizationReadinessRegistry.cs',
    'config/azure-runtime/workers/stabilization-readiness.sample.json'
)

foreach ($file in $expectedFiles) {
    Assert-FileExists -RelativePath $file
}

Assert-FileContains -RelativePath 'src/Core/MigrationBase.Core/Cloud/Azure/Workers/Stabilization/AzureWorkerStabilizationReadinessRegistry.cs' -Needle 'P5.2.10'
Assert-FileContains -RelativePath 'config/azure-runtime/workers/stabilization-readiness.sample.json' -Needle 'P5.3 Deployment Automation'

$projectFiles = @(Get-ChildItem -LiteralPath $repoRootPath -Recurse -Filter '*.csproj' -File | Where-Object {
    $_.FullName -notmatch '\\bin\\' -and $_.FullName -notmatch '\\obj\\'
})

$inlineVersionViolations = @()
foreach ($projectFile in $projectFiles) {
    $projectText = Get-Content -LiteralPath $projectFile.FullName -Raw
    if ($projectText -match '<PackageReference\b[^>]*\bVersion\s*=') {
        $inlineVersionViolations += $projectFile.FullName
    }
}

if (@($inlineVersionViolations).Length -gt 0) {
    Write-Host 'Inline PackageReference Version attributes detected:'
    foreach ($violation in $inlineVersionViolations) {
        Write-Host " - $violation"
    }
    throw 'Central package management convention may be violated.'
}

$migrationBaseProject = Join-RepoPath 'src/Core/MigrationBase.Core/MigrationBase.Core.csproj'
if (Test-Path -LiteralPath $migrationBaseProject -PathType Leaf) {
    Write-Host 'Restoring MigrationBase.Core...'
    dotnet restore $migrationBaseProject
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

    Write-Host 'Building MigrationBase.Core...'
    dotnet build $migrationBaseProject --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }
}
else {
    throw 'MigrationBase.Core project file is missing.'
}

Write-Host 'P5.2.11 Azure worker stabilization closeout validation passed.'
