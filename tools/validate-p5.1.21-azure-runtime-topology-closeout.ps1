Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-ScriptDirectory {
    if (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) { return $PSScriptRoot }
    if (-not [string]::IsNullOrWhiteSpace($PSCommandPath)) { return Split-Path -Parent $PSCommandPath }
    return (Get-Location).Path
}

function Assert-FileExists {
    param([Parameter(Mandatory=$true)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Missing expected file: ${Path}"
    }
}

function Get-XmlDocument {
    param([Parameter(Mandatory=$true)][string]$Path)
    $xml = New-Object System.Xml.XmlDocument
    $xml.PreserveWhitespace = $true
    $xml.Load($Path)
    return $xml
}

$scriptDirectory = Get-ScriptDirectory
$repoRoot = Split-Path -Parent $scriptDirectory

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Operationalization\AzureRuntimeTopologyReadinessStatus.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Operationalization\AzureRuntimeTopologyClosureGate.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Operationalization\AzureRuntimeTopologyReadinessSummary.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Operationalization\AzureRuntimeTopologyClosureChecklist.cs',
    'config\azure-runtime\readiness\p5.1-topology-closeout.sample.json'
)

foreach ($relativePath in $expectedFiles) {
    Assert-FileExists -Path (Join-Path $repoRoot $relativePath)
}

$projectFiles = @(Get-ChildItem -LiteralPath $repoRoot -Recurse -Filter '*.csproj' -File |
    Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\' })

$badProjectFiles = @()
foreach ($projectFile in $projectFiles) {
    $xml = Get-XmlDocument -Path $projectFile.FullName
    $inlineVersions = @($xml.SelectNodes('//PackageReference[@Version]'))
    if ($inlineVersions.Count -gt 0) {
        $badProjectFiles += $projectFile.FullName
    }
}

if (@($badProjectFiles).Count -gt 0) {
    Write-Host 'Inline PackageReference Version attributes detected:'
    foreach ($badProjectFile in $badProjectFiles) { Write-Host " - $badProjectFile" }
    throw 'Central package management convention may be violated.'
}

$coreProject = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (Test-Path -LiteralPath $coreProject -PathType Leaf) {
    Write-Host 'Restoring MigrationBase.Core...'
    dotnet restore $coreProject
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

    Write-Host 'Building MigrationBase.Core...'
    dotnet build $coreProject --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }
}

Write-Host 'P5.1.21 Azure runtime topology closeout validation passed.'
