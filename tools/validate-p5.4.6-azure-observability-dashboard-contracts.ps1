Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-ScriptDirectory {
    if (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) { return $PSScriptRoot }
    if ($MyInvocation.MyCommand.Path) { return Split-Path -Parent $MyInvocation.MyCommand.Path }
    return (Get-Location).Path
}

function Assert-FileExists {
    param([Parameter(Mandatory=$true)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Missing expected file: $Path"
    }
}

function Get-ProjectPackageReferences {
    param([Parameter(Mandatory=$true)][string]$ProjectPath)

    [xml]$projectXml = Get-Content -LiteralPath $ProjectPath -Raw
    $projectNode = $projectXml.Project
    if ($null -eq $projectNode) { return @() }
    if (-not $projectNode.PSObject.Properties.Match('ItemGroup').Count) { return @() }

    $results = New-Object System.Collections.Generic.List[object]
    foreach ($itemGroup in @($projectNode.ItemGroup)) {
        if ($null -eq $itemGroup) { continue }
        if (-not $itemGroup.PSObject.Properties.Match('PackageReference').Count) { continue }

        foreach ($packageRef in @($itemGroup.PackageReference)) {
            if ($null -eq $packageRef) { continue }
            $include = $null
            $version = $null
            if ($packageRef.PSObject.Properties.Match('Include').Count) { $include = [string]$packageRef.Include }
            if ($packageRef.PSObject.Properties.Match('Version').Count) { $version = [string]$packageRef.Version }
            $results.Add([pscustomobject]@{ Include = $include; Version = $version }) | Out-Null
        }
    }

    return @($results.ToArray())
}

$scriptDirectory = Get-ScriptDirectory
$repoRoot = Split-Path -Parent $scriptDirectory
$coreProject = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Observability\Dashboards\AzureDashboardDescriptor.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Observability\Dashboards\AzureDashboardPanelDescriptor.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Observability\Dashboards\AzureDashboardRegistry.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Observability\Dashboards\IAzureDashboardRegistry.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Observability\Dashboards\AzureDashboardValidationResult.cs',
    'config\azure-runtime\observability\dashboards\dashboards.sample.json'
)

foreach ($relativePath in $expectedFiles) {
    Assert-FileExists -Path (Join-Path $repoRoot $relativePath)
}

$badPackageReferences = New-Object System.Collections.Generic.List[string]
$projectFiles = @(Get-ChildItem -LiteralPath (Join-Path $repoRoot 'src') -Recurse -Filter '*.csproj' -File | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' })
foreach ($project in $projectFiles) {
    foreach ($packageReference in @(Get-ProjectPackageReferences -ProjectPath $project.FullName)) {
        if (-not [string]::IsNullOrWhiteSpace($packageReference.Version)) {
            $badPackageReferences.Add($project.FullName) | Out-Null
            break
        }
    }
}

if (@($badPackageReferences).Length -gt 0) {
    Write-Host 'Inline PackageReference Version attributes detected:'
    foreach ($bad in @($badPackageReferences)) { Write-Host " - $bad" }
    throw 'Central package management convention violation detected.'
}

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $coreProject
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build $coreProject --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'P5.4.6 Azure observability dashboard contract validation passed.'
