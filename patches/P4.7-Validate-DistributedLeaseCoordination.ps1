[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Step {
    param([string] $Message)
    Write-Host "[P4.7-VALIDATE] $Message"
}

function Get-RepoRoot {
    $scriptRoot = Split-Path -Parent $PSCommandPath
    return (Resolve-Path (Join-Path $scriptRoot '..')).Path
}

function Assert-FileExists {
    param([string] $Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Expected file not found: $Path"
    }
    Write-Step "Found $Path"
}

function Assert-TextContains {
    param([string] $Path, [string] $Text)
    $content = Get-Content -LiteralPath $Path -Raw
    if ($content.IndexOf($Text, [System.StringComparison]::Ordinal) -lt 0) {
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

    Write-Step "No inline package versions: $ProjectPath"
}

$repoRoot = Get-RepoRoot
Set-Location $repoRoot
Write-Step "Repo root: $repoRoot"

$expectedFiles = @(
    'src\Core\Migration.Application\Operational\Leases\OperationalWorkItemLeaseContracts.cs',
    'src\Core\Migration.Infrastructure.Sql\Operational\Leases\SqlOperationalWorkItemLeaseCoordinator.cs',
    'src\Core\Migration.Infrastructure.Sql\Operational\Leases\SqlOperationalWorkItemLeaseCoordinatorOptions.cs',
    'src\Core\Migration.Infrastructure.Sql\Operational\Leases\SqlOperationalWorkItemLeaseCoordinatorServiceCollectionExtensions.cs',
    'database\sql\operational\004_operational_work_item_lease_indexes.sql',
    'docs\cloud-roadmap-cleanup\P4_SET_007_DISTRIBUTED_LEASE_COORDINATION.md'
)

foreach ($relativeFile in $expectedFiles) {
    Assert-FileExists (Join-Path $repoRoot $relativeFile)
}

Assert-TextContains (Join-Path $repoRoot 'src\Core\Migration.Application\Operational\Leases\OperationalWorkItemLeaseContracts.cs') 'IOperationalWorkItemLeaseCoordinator'
Assert-TextContains (Join-Path $repoRoot 'src\Core\Migration.Infrastructure.Sql\Operational\Leases\SqlOperationalWorkItemLeaseCoordinator.cs') 'ReleaseExpiredLeasesAsync'
Assert-TextContains (Join-Path $repoRoot 'database\sql\operational\004_operational_work_item_lease_indexes.sql') 'IX_OperationalWorkItems_LeaseExpiration'

Assert-NoInlinePackageVersions (Join-Path $repoRoot 'src\Core\Migration.Infrastructure.Sql\Migration.Infrastructure.Sql.csproj')

Write-Step 'Validation complete.'
