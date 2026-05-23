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

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Governance\ProductionGovernanceCloseoutStatus.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Governance\ProductionGovernanceHandoffArea.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Governance\ProductionGovernanceHandoffItem.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Governance\ProductionGovernanceCloseoutManifest.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Governance\ProductionGovernanceCloseoutResult.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Governance\IProductionGovernanceCloseoutEvaluator.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Governance\ProductionGovernanceCloseoutEvaluator.cs',
    'config\azure-runtime\governance\production-governance-closeout.sample.json'
)

$missing = @()
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) { $missing += $relativePath }
}

if (@($missing).Length -gt 0) {
    Write-Host 'Missing expected P5.6.6 files:' -ForegroundColor Red
    foreach ($path in $missing) { Write-Host " - $path" -ForegroundColor Red }
    exit 1
}

function Get-XmlAttributeValue {
    param(
        [Parameter(Mandatory = $true)] $Node,
        [Parameter(Mandatory = $true)] [string] $Name
    )

    if ($null -eq $Node) { return $null }
    if ($null -eq $Node.Attributes) { return $null }
    $attribute = $Node.Attributes[$Name]
    if ($null -eq $attribute) { return $null }
    return $attribute.Value
}

$projectFiles = @(Get-ChildItem -LiteralPath $repoRoot -Recurse -Filter '*.csproj' -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' })

$inlineVersions = @()
foreach ($project in $projectFiles) {
    [xml]$projectXml = Get-Content -LiteralPath $project.FullName -Raw
    $itemGroups = @()
    if ($projectXml.PSObject.Properties['Project'] -and $projectXml.Project.PSObject.Properties['ItemGroup']) {
        $itemGroups = @($projectXml.Project.ItemGroup)
    }

    foreach ($itemGroup in $itemGroups) {
        if ($null -eq $itemGroup) { continue }
        if (-not $itemGroup.PSObject.Properties['PackageReference']) { continue }
        foreach ($packageRef in @($itemGroup.PackageReference)) {
            if ($null -eq $packageRef) { continue }
            $version = Get-XmlAttributeValue -Node $packageRef -Name 'Version'
            if (-not [string]::IsNullOrWhiteSpace($version)) { $inlineVersions += $project.FullName }
        }
    }
}

if (@($inlineVersions).Length -gt 0) {
    Write-Host 'Inline PackageReference Version attributes detected:' -ForegroundColor Red
    foreach ($path in @($inlineVersions | Sort-Object -Unique)) { Write-Host " - $path" -ForegroundColor Red }
    exit 1
}

$coreProject = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (-not (Test-Path -LiteralPath $coreProject)) { throw "Missing project: $coreProject" }

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $coreProject --nologo
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build $coreProject --nologo --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'P5.6.6 production governance closeout validation passed.' -ForegroundColor Green
