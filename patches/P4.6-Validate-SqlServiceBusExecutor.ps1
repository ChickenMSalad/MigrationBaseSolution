[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $scriptRoot = Split-Path -Parent $PSCommandPath
    return (Resolve-Path (Join-Path $scriptRoot '..')).Path
}

function Assert-PathExists {
    param([string] $Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        throw ("Expected path not found: {0}" -f $Path)
    }
}

function Assert-FileContains {
    param([string] $Path, [string] $Text)
    Assert-PathExists $Path
    $content = Get-Content -LiteralPath $Path -Raw
    if ($content.IndexOf($Text, [StringComparison]::Ordinal) -lt 0) {
        throw ("Expected text not found in {0}: {1}" -f $Path, $Text)
    }
}

function Assert-NoInlinePackageVersions {
    param([string] $ProjectPath)
    [xml] $project = Get-Content -LiteralPath $ProjectPath -Raw
    $packageReferences = @()

    if ($project.Project.PSObject.Properties.Name -contains 'ItemGroup') {
        foreach ($itemGroup in @($project.Project.ItemGroup)) {
            if ($itemGroup.PSObject.Properties.Name -contains 'PackageReference') {
                $packageReferences += @($itemGroup.PackageReference)
            }
        }
    }

    foreach ($reference in $packageReferences) {
        if ($reference.PSObject.Properties.Name -contains 'Version') {
            throw ("Inline package version found in {0}" -f $ProjectPath)
        }
    }
}

$repoRoot = Get-RepoRoot
Set-Location $repoRoot

$projectPath = Join-Path $repoRoot 'src\Workers\Migration.Workers.ServiceBusExecutor\Migration.Workers.ServiceBusExecutor.csproj'
Assert-PathExists $projectPath
Assert-NoInlinePackageVersions $projectPath
Assert-FileContains $projectPath '..\..\Core\Migration.Application\Migration.Application.csproj'
Assert-FileContains $projectPath '..\..\Core\Migration.Infrastructure.Sql\Migration.Infrastructure.Sql.csproj'
Assert-PathExists (Join-Path $repoRoot 'src\Workers\Migration.Workers.ServiceBusExecutor\Runtime\SqlServiceBusExecutorWorker.cs')
Assert-PathExists (Join-Path $repoRoot 'src\Workers\Migration.Workers.ServiceBusExecutor\Processing\PlaceholderServiceBusWorkItemExecutor.cs')
Assert-PathExists (Join-Path $repoRoot 'config-samples\appsettings.SqlServiceBusExecutor.sample.json')
Assert-PathExists (Join-Path $repoRoot 'docs\cloud-roadmap-cleanup\P4_SET_006_SQL_SERVICE_BUS_EXECUTOR.md')

$solutionList = & dotnet sln (Join-Path $repoRoot 'MigrationBaseSolution.sln') list
$found = $false
foreach ($line in $solutionList) {
    if ($line -like '*Migration.Workers.ServiceBusExecutor.csproj') {
        $found = $true
        break
    }
}
if (-not $found) {
    throw 'Migration.Workers.ServiceBusExecutor is not listed in the solution.'
}

Write-Host '[P4.6] Validation passed.'
