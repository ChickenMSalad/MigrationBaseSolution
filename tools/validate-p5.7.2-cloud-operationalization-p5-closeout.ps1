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
if ([string]::IsNullOrWhiteSpace($repoRoot) -or -not (Test-Path -LiteralPath $repoRoot)) {
    throw 'Unable to resolve repository root.'
}

$expectedFiles = @(
    'src/Core/MigrationBase.Core/Cloud/Azure/Readiness/CloudOperationalizationCloseoutStatus.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Readiness/CloudOperationalizationCloseoutItem.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Readiness/CloudOperationalizationCloseoutSummary.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Readiness/CloudOperationalizationCloseoutAreas.cs',
    'config/azure-runtime/readiness/p5-closeout.sample.json'
)

$missingFiles = @()
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        $missingFiles += $relativePath
    }
}

if (@($missingFiles).Length -gt 0) {
    Write-Host 'Missing expected P5.7.2 files:'
    foreach ($missingFile in $missingFiles) { Write-Host " - $missingFile" }
    exit 1
}

$projectFiles = @(Get-ChildItem -LiteralPath $repoRoot -Recurse -Filter '*.csproj' -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' })

$inlineVersions = @()
foreach ($projectFile in $projectFiles) {
    $content = Get-Content -LiteralPath $projectFile.FullName -Raw
    if ($content -match '<PackageReference\b[^>]*\bVersion\s*=') {
        $inlineVersions += $projectFile.FullName
    }
}

if (@($inlineVersions).Length -gt 0) {
    Write-Host 'Inline PackageReference Version attributes detected:'
    foreach ($path in $inlineVersions) { Write-Host " - $path" }
    exit 1
}

$coreProject = Join-Path $repoRoot 'src/Core/MigrationBase.Core/MigrationBase.Core.csproj'
if (Test-Path -LiteralPath $coreProject -PathType Leaf) {
    Write-Host 'Restoring MigrationBase.Core...'
    dotnet restore $coreProject
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

    Write-Host 'Building MigrationBase.Core...'
    dotnet build $coreProject --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }
}
else {
    throw 'MigrationBase.Core project file was not found.'
}

Write-Host 'P5.7.2 cloud operationalization P5 closeout validation passed.'
