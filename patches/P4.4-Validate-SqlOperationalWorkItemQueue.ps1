[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-P44([string]$Message) {
    Write-Host "[P4.4-VALIDATE] $Message"
}

function Get-RepoRoot {
    $current = (Get-Location).Path
    while ($true) {
        if (Test-Path -LiteralPath (Join-Path $current 'MigrationBaseSolution.sln')) {
            return $current
        }

        $parent = Split-Path -Parent $current
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $current) {
            throw 'Could not locate MigrationBaseSolution.sln. Run this script from the repo root or a child folder.'
        }

        $current = $parent
    }
}

function Assert-FileExists([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Expected file not found: $Path"
    }

    Write-P44 "Found $Path"
}

function Assert-Text([string]$Path, [string]$Text) {
    Assert-FileExists $Path
    $content = Get-Content -LiteralPath $Path -Raw
    if (-not $content.Contains($Text)) {
        throw ('Expected text not found in {0}: {1}' -f $Path, $Text)
    }

    Write-P44 "Verified text in $Path"
}

function Assert-NoInlinePackageVersions([string]$ProjectPath) {
    Assert-FileExists $ProjectPath
    [xml]$project = Get-Content -LiteralPath $ProjectPath -Raw

    foreach ($itemGroup in @($project.Project.ItemGroup)) {
        if ($itemGroup.PSObject.Properties.Name -contains 'PackageReference') {
            foreach ($reference in @($itemGroup.PackageReference)) {
                if ($reference.PSObject.Properties.Name -contains 'Version') {
                    throw "Inline PackageReference Version found in $ProjectPath"
                }
            }
        }
    }

    Write-P44 "No inline PackageReference versions in $ProjectPath"
}

$repoRoot = Get-RepoRoot
Write-P44 "Repo root: $repoRoot"

$paths = @(
    'src/Core/Migration.Application/Operational/WorkItems/OperationalWorkItemQueueContracts.cs',
    'src/Core/Migration.Infrastructure.Sql/Operational/WorkItems/SqlOperationalWorkItemQueueOptions.cs',
    'src/Core/Migration.Infrastructure.Sql/Operational/WorkItems/SqlOperationalWorkItemQueue.cs',
    'src/Core/Migration.Infrastructure.Sql/Operational/WorkItems/SqlOperationalWorkItemQueueServiceCollectionExtensions.cs',
    'src/Core/Migration.Admin.Api/Endpoints/Operational/SqlBackbone/SqlOperationalWorkItemQueueEndpointExtensions.cs',
    'database/sql/operational/003_create_operational_work_item_queue.sql'
)

foreach ($relativePath in $paths) {
    Assert-FileExists (Join-Path $repoRoot $relativePath)
}

Assert-Text (Join-Path $repoRoot 'src/Core/Migration.Admin.Api/Program.cs') 'builder.Services.AddSqlOperationalWorkItemQueue();'
Assert-Text (Join-Path $repoRoot 'src/Core/Migration.Admin.Api/Program.cs') 'app.MapSqlOperationalWorkItemQueueEndpoints();'
Assert-Text (Join-Path $repoRoot 'src/Core/Migration.Infrastructure.Sql/Migration.Infrastructure.Sql.csproj') '..\Migration.Application\Migration.Application.csproj'
Assert-Text (Join-Path $repoRoot 'src/Core/Migration.Admin.Api/Migration.Admin.Api.csproj') '..\Migration.Infrastructure.Sql\Migration.Infrastructure.Sql.csproj'

Assert-NoInlinePackageVersions (Join-Path $repoRoot 'src/Core/Migration.Infrastructure.Sql/Migration.Infrastructure.Sql.csproj')
Assert-NoInlinePackageVersions (Join-Path $repoRoot 'src/Core/Migration.Admin.Api/Migration.Admin.Api.csproj')

Write-P44 'Validation complete.'
