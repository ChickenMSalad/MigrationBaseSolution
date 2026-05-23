Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDirectory = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $PSCommandPath }
$repoRoot = (Resolve-Path (Join-Path $scriptDirectory '..')).Path

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Governance\AzureProductionReadinessGate.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Governance\AzureProductionReadinessGateCategory.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Governance\AzureProductionReadinessGateSeverity.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Governance\AzureProductionReadinessAssessment.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Governance\AzureProductionReadinessGateResult.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Governance\AzureProductionReadinessGateStatus.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Governance\IAzureProductionReadinessGateRegistry.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Governance\AzureProductionReadinessGateRegistry.cs',
    'config\azure-runtime\governance\production-readiness.gates.sample.json'
)

$missing = @()
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        $missing += $relativePath
    }
}

if (@($missing).Count -gt 0) {
    Write-Host 'Missing expected P5.6.1 files:' -ForegroundColor Red
    foreach ($item in $missing) { Write-Host " - $item" -ForegroundColor Red }
    exit 1
}

$projectPath = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) {
    throw "Missing project file: ${projectPath}"
}

function Get-ProjectPackageReferences {
    param([Parameter(Mandatory=$true)][string]$ProjectPath)

    [xml]$projectXml = Get-Content -LiteralPath $ProjectPath -Raw
    if ($null -eq $projectXml.Project) { return @() }
    if (-not $projectXml.Project.PSObject.Properties['ItemGroup']) { return @() }

    $results = @()
    foreach ($itemGroup in @($projectXml.Project.ItemGroup)) {
        if ($null -eq $itemGroup) { continue }
        if (-not $itemGroup.PSObject.Properties['PackageReference']) { continue }

        foreach ($packageReference in @($itemGroup.PackageReference)) {
            if ($null -eq $packageReference) { continue }
            $include = $null
            $version = $null
            if ($packageReference.PSObject.Properties['Include']) { $include = [string]$packageReference.Include }
            if ($packageReference.PSObject.Properties['Version']) { $version = [string]$packageReference.Version }
            if (-not [string]::IsNullOrWhiteSpace($include)) {
                $results += [pscustomobject]@{ Include = $include; Version = $version; ProjectPath = $ProjectPath }
            }
        }
    }

    return @($results)
}

$projects = @(Get-ChildItem -LiteralPath (Join-Path $repoRoot 'src') -Recurse -Filter '*.csproj' -File | Where-Object {
    $_.FullName -notmatch '\\bin\\' -and $_.FullName -notmatch '\\obj\\'
})

$badPackageRefs = @()
foreach ($project in $projects) {
    foreach ($packageReference in @(Get-ProjectPackageReferences -ProjectPath $project.FullName)) {
        if (-not [string]::IsNullOrWhiteSpace($packageReference.Version)) {
            $badPackageRefs += $packageReference
        }
    }
}

if (@($badPackageRefs).Count -gt 0) {
    Write-Host 'Inline PackageReference Version attributes detected:' -ForegroundColor Red
    foreach ($bad in $badPackageRefs) {
        Write-Host (" - {0}: {1}" -f $bad.ProjectPath, $bad.Include) -ForegroundColor Red
    }
    exit 1
}

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $projectPath
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build $projectPath --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'P5.6.1 production readiness governance contract validation passed.' -ForegroundColor Green
