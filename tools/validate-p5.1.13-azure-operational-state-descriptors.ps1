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
    'src\Core\MigrationBase.Core\Cloud\Azure\Operations\AzureOperationalStateKind.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Operations\AzureOperationalStateSeverity.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Operations\AzureOperationalStateSignal.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Operations\AzureOperationalStateDescriptor.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Operations\AzureOperationalStateEvaluator.cs',
    'config\azure-runtime\operations\operational-state.sample.json'
)

$missing = @()
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        $missing += $relativePath
    }
}

if (@($missing).Length -gt 0) {
    Write-Host 'Missing expected P5.1.13 files:' -ForegroundColor Red
    foreach ($item in $missing) {
        Write-Host " - $item" -ForegroundColor Red
    }
    exit 1
}

$coreProject = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
Assert-FileExists -Path $coreProject

$projectFiles = @(Get-ChildItem -LiteralPath $repoRoot -Recurse -Filter '*.csproj' -File |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' })

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

$samplePath = Join-Path $repoRoot 'config\azure-runtime\operations\operational-state.sample.json'
$sampleJson = Get-Content -LiteralPath $samplePath -Raw | ConvertFrom-Json
if ([string]::IsNullOrWhiteSpace($sampleJson.environmentName)) { throw 'Sample operational state must include environmentName.' }
if ([string]::IsNullOrWhiteSpace($sampleJson.scopeName)) { throw 'Sample operational state must include scopeName.' }
if ($null -eq $sampleJson.signals) { throw 'Sample operational state must include signals.' }
$signals = @($sampleJson.signals | Where-Object { $null -ne $_ })
if (@($signals).Length -lt 3) { throw 'Sample operational state should include at least three signals.' }

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

Write-Host 'P5.1.13 Azure operational state descriptor validation passed.' -ForegroundColor Green
