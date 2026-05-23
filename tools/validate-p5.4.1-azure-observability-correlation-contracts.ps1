Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptPath = $MyInvocation.MyCommand.Path
    if ([string]::IsNullOrWhiteSpace($scriptPath)) { $scriptPath = $PSCommandPath }
    if ([string]::IsNullOrWhiteSpace($scriptPath)) { throw 'Unable to determine script path.' }
    $scriptDirectory = Split-Path -Parent $scriptPath
}

$repoRoot = Split-Path -Parent $scriptDirectory

$expectedFiles = @(
    'src/Core/MigrationBase.Core/Cloud/Azure/Observability/AzureCorrelationScopeDescriptor.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Observability/AzureCorrelationContext.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Observability/AzureCorrelationScopeFactory.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Observability/AzureTelemetryDimensionNames.cs',
    'config/azure-runtime/observability/correlation.sample.json'
)

$missing = @()
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        $missing += $relativePath
    }
}

if (@($missing).Length -gt 0) {
    Write-Host 'Missing expected P5.4.1 files:' -ForegroundColor Red
    foreach ($item in $missing) { Write-Host " - $item" -ForegroundColor Red }
    throw 'P5.4.1 validation failed.'
}

$projectFiles = @(Get-ChildItem -LiteralPath $repoRoot -Recurse -Filter '*.csproj' -File | Where-Object {
    $_.FullName -notmatch '\\bin\\' -and $_.FullName -notmatch '\\obj\\'
})

$inlineVersions = @()
foreach ($project in $projectFiles) {
    $content = Get-Content -LiteralPath $project.FullName -Raw
    if ($content -match '<PackageReference\b[^>]*\bVersion\s*=') {
        $inlineVersions += $project.FullName
    }
}

if (@($inlineVersions).Length -gt 0) {
    Write-Host 'Inline PackageReference Version attributes detected:' -ForegroundColor Red
    foreach ($path in $inlineVersions) { Write-Host " - $path" -ForegroundColor Red }
    throw 'Central package management convention may be violated.'
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

Write-Host 'P5.4.1 Azure observability correlation contract validation passed.' -ForegroundColor Green
