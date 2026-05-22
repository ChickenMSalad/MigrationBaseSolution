[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [switch] $Apply
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Step {
    param([string] $Message)
    Write-Host "[P4.7] $Message"
}

function Get-RepoRoot {
    $scriptRoot = Split-Path -Parent $PSCommandPath
    return (Resolve-Path (Join-Path $scriptRoot '..')).Path
}

function Ensure-Directory {
    param([string] $Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        if ($Apply) {
            New-Item -ItemType Directory -Path $Path | Out-Null
            Write-Step "Created $Path"
        } else {
            Write-Step "WOULD create $Path"
        }
    }
}

function Copy-FileSafe {
    param([string] $Source, [string] $Destination)

    if (-not (Test-Path -LiteralPath $Source)) {
        throw "Payload source not found: $Source"
    }

    if (Test-Path -LiteralPath $Destination) {
        Write-Step "Already exists, leaving unchanged: $Destination"
        return
    }

    if ($Apply) {
        Ensure-Directory (Split-Path -Parent $Destination)
        Copy-Item -LiteralPath $Source -Destination $Destination
        Write-Step "Copied $Destination"
    } else {
        Write-Step "WOULD copy $Source -> $Destination"
    }
}

$repoRoot = Get-RepoRoot
Set-Location $repoRoot
Write-Step "Repo root: $repoRoot"

$payloadRoot = Join-Path $repoRoot 'payload'

$requiredProjects = @(
    'src\Core\Migration.Application\Migration.Application.csproj',
    'src\Core\Migration.Infrastructure.Sql\Migration.Infrastructure.Sql.csproj'
)

foreach ($relativeProject in $requiredProjects) {
    $projectPath = Join-Path $repoRoot $relativeProject
    if (-not (Test-Path -LiteralPath $projectPath)) {
        throw "Expected project not found: $projectPath"
    }
}

$files = @(
    'src\Core\Migration.Application\Operational\Leases\OperationalWorkItemLeaseContracts.cs',
    'src\Core\Migration.Infrastructure.Sql\Operational\Leases\SqlOperationalWorkItemLeaseCoordinator.cs',
    'src\Core\Migration.Infrastructure.Sql\Operational\Leases\SqlOperationalWorkItemLeaseCoordinatorOptions.cs',
    'src\Core\Migration.Infrastructure.Sql\Operational\Leases\SqlOperationalWorkItemLeaseCoordinatorServiceCollectionExtensions.cs',
    'database\sql\operational\004_operational_work_item_lease_indexes.sql',
    'docs\cloud-roadmap-cleanup\P4_SET_007_DISTRIBUTED_LEASE_COORDINATION.md'
)

foreach ($relativeFile in $files) {
    $source = Join-Path $payloadRoot $relativeFile
    $destination = Join-Path $repoRoot $relativeFile
    Copy-FileSafe $source $destination
}

if (-not $Apply) {
    Write-Step 'Preview only. Re-run with -Apply to install.'
} else {
    Write-Step 'Complete. Next: ./patches/P4.7-Validate-DistributedLeaseCoordination.ps1; dotnet restore; dotnet build'
}
