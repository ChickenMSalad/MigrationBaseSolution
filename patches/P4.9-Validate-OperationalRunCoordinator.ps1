[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Step {
    param([Parameter(Mandatory = $true)][string] $Message)
    Write-Host ("[P4.9-VALIDATE] {0}" -f $Message)
}

function Assert-FileExists {
    param([Parameter(Mandatory = $true)][string] $Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        throw ("Expected file not found: {0}" -f $Path)
    }
    Write-Step ("Found {0}" -f $Path)
}

function Assert-TextContains {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Text
    )
    $content = Get-Content -LiteralPath $Path -Raw
    if ($content -notmatch [regex]::Escape($Text)) {
        throw ("Expected text not found in {0}: {1}" -f $Path, $Text)
    }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Set-Location $repoRoot

$expectedFiles = @(
    'src\Core\Migration.Application\Operational\Runs\OperationalRunCoordinatorContracts.cs',
    'src\Core\Migration.Infrastructure.Sql\Operational\Runs\SqlOperationalRunCoordinator.cs',
    'src\Core\Migration.Infrastructure.Sql\Operational\Runs\SqlOperationalRunCoordinatorOptions.cs',
    'src\Core\Migration.Infrastructure.Sql\Operational\Runs\SqlOperationalRunCoordinatorServiceCollectionExtensions.cs',
    'src\Core\Migration.Admin.Api\Endpoints\Operational\SqlBackbone\SqlOperationalRunCoordinatorEndpointExtensions.cs',
    'src\Core\Migration.Admin.Api\Registration\AdminApiOperationalRunCoordinatorRegistrationExtensions.cs',
    'database\sql\operational\005_operational_run_coordinator.sql',
    'docs\cloud-roadmap-cleanup\P4_SET_009_OPERATIONAL_RUN_COORDINATOR.md'
)

foreach ($relativeFile in $expectedFiles) {
    Assert-FileExists -Path (Join-Path $repoRoot $relativeFile)
}

$programPath = Join-Path $repoRoot 'src\Core\Migration.Admin.Api\Program.cs'
Assert-TextContains -Path $programPath -Text 'builder.Services.AddAdminApiOperationalRunCoordinator(builder.Configuration);'
Assert-TextContains -Path $programPath -Text 'app.MapSqlOperationalRunCoordinatorEndpoints();'

$contractsPath = Join-Path $repoRoot 'src\Core\Migration.Application\Operational\Runs\OperationalRunCoordinatorContracts.cs'
Assert-TextContains -Path $contractsPath -Text 'public interface IOperationalRunCoordinator'

$sqlCoordinatorPath = Join-Path $repoRoot 'src\Core\Migration.Infrastructure.Sql\Operational\Runs\SqlOperationalRunCoordinator.cs'
Assert-TextContains -Path $sqlCoordinatorPath -Text 'public sealed class SqlOperationalRunCoordinator : IOperationalRunCoordinator'

Write-Step 'Validation passed. Next: dotnet restore; dotnet build'
