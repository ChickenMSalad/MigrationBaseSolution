[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory = $false)]
    [switch] $Apply
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Step {
    param([Parameter(Mandatory = $true)][string] $Message)
    Write-Host ("[P4.9] {0}" -f $Message)
}

function Get-RepoRoot {
    $scriptRoot = Split-Path -Parent $PSCommandPath
    return (Resolve-Path (Join-Path $scriptRoot '..')).Path
}

function Copy-FileSafe {
    param(
        [Parameter(Mandatory = $true)][string] $Source,
        [Parameter(Mandatory = $true)][string] $Destination
    )

    if (-not (Test-Path -LiteralPath $Source)) {
        throw ("Payload source not found: {0}" -f $Source)
    }

    $destinationDirectory = Split-Path -Parent $Destination
    if (-not (Test-Path -LiteralPath $destinationDirectory)) {
        if ($Apply) {
            New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
            Write-Step ("Created {0}" -f $destinationDirectory)
        }
        else {
            Write-Step ("WOULD create {0}" -f $destinationDirectory)
        }
    }

    if ($Apply) {
        Copy-Item -LiteralPath $Source -Destination $Destination -Force
        Write-Step ("Copied {0}" -f $Destination.Substring($repoRoot.Length + 1))
    }
    else {
        Write-Step ("WOULD copy {0} -> {1}" -f $Source, $Destination)
    }
}

function Ensure-Using {
    param(
        [Parameter(Mandatory = $true)][string] $Content,
        [Parameter(Mandatory = $true)][string] $UsingStatement
    )

    if ($Content -match [regex]::Escape($UsingStatement)) {
        return $Content
    }

    return $UsingStatement + [Environment]::NewLine + $Content
}

function Ensure-ServiceRegistrationAfterBuilder {
    param(
        [Parameter(Mandatory = $true)][string] $Content,
        [Parameter(Mandatory = $true)][string] $Line
    )

    if ($Content -match [regex]::Escape($Line)) {
        return $Content
    }

    $pattern = 'var\s+builder\s*=\s*WebApplication\.CreateBuilder\(args\)\s*;'
    if ($Content -notmatch $pattern) {
        throw ("Could not find WebApplication builder creation in Program.cs. Add manually after builder creation: {0}" -f $Line)
    }

    return [regex]::Replace($Content, $pattern, ('$0' + [Environment]::NewLine + $Line), 1)
}

function Ensure-LineBeforeAppRun {
    param(
        [Parameter(Mandatory = $true)][string] $Content,
        [Parameter(Mandatory = $true)][string] $Line
    )

    if ($Content -match [regex]::Escape($Line)) {
        return $Content
    }

    if ($Content -notmatch 'app\.Run\s*\(') {
        throw ("Could not find app.Run(...) in Program.cs. Add manually before app.Run(...): {0}" -f $Line)
    }

    return [regex]::Replace($Content, 'app\.Run\s*\(', ($Line + [Environment]::NewLine + 'app.Run('), 1)
}



$repoRoot = Get-RepoRoot
$payloadRoot = Join-Path $repoRoot 'payload'
Set-Location $repoRoot

Write-Step ("Repo root: {0}" -f $repoRoot)

$requiredProjects = @(
    'src\Core\Migration.Application\Migration.Application.csproj',
    'src\Core\Migration.Infrastructure.Sql\Migration.Infrastructure.Sql.csproj',
    'src\Core\Migration.Admin.Api\Migration.Admin.Api.csproj'
)

foreach ($relativeProject in $requiredProjects) {
    $projectPath = Join-Path $repoRoot $relativeProject
    if (-not (Test-Path -LiteralPath $projectPath)) {
        throw ("Expected project not found: {0}" -f $projectPath)
    }
}

$files = @(
    'src\Core\Migration.Application\Operational\Runs\OperationalRunCoordinatorContracts.cs',
    'src\Core\Migration.Infrastructure.Sql\Operational\Runs\SqlOperationalRunCoordinator.cs',
    'src\Core\Migration.Infrastructure.Sql\Operational\Runs\SqlOperationalRunCoordinatorOptions.cs',
    'src\Core\Migration.Infrastructure.Sql\Operational\Runs\SqlOperationalRunCoordinatorServiceCollectionExtensions.cs',
    'src\Core\Migration.Admin.Api\Endpoints\Operational\SqlBackbone\SqlOperationalRunCoordinatorEndpointExtensions.cs',
    'src\Core\Migration.Admin.Api\Registration\AdminApiOperationalRunCoordinatorRegistrationExtensions.cs',
    'database\sql\operational\005_operational_run_coordinator.sql',
    'docs\cloud-roadmap-cleanup\P4_SET_009_OPERATIONAL_RUN_COORDINATOR.md'
)

foreach ($relativeFile in $files) {
    $source = Join-Path $payloadRoot $relativeFile
    $destination = Join-Path $repoRoot $relativeFile
    Copy-FileSafe -Source $source -Destination $destination
}

$programPath = Join-Path $repoRoot 'src\Core\Migration.Admin.Api\Program.cs'
if (-not (Test-Path -LiteralPath $programPath)) {
    throw ("Program.cs not found: {0}" -f $programPath)
}

if ($Apply) {
    $program = Get-Content -LiteralPath $programPath -Raw
    $program = Ensure-Using -Content $program -UsingStatement 'using Migration.Admin.Api.Endpoints.Operational.SqlBackbone;'
    $program = Ensure-Using -Content $program -UsingStatement 'using Migration.Admin.Api.Registration;'
    $program = Ensure-ServiceRegistrationAfterBuilder -Content $program -Line 'builder.Services.AddAdminApiOperationalRunCoordinator(builder.Configuration);'
    $program = Ensure-LineBeforeAppRun -Content $program -Line 'app.MapSqlOperationalRunCoordinatorEndpoints();'
    Set-Content -LiteralPath $programPath -Value $program -Encoding UTF8
    Write-Step 'Updated Program.cs registration'
}
else {
    Write-Step 'WOULD add Admin API service and endpoint registration to Program.cs if missing'
}

if (-not $Apply) {
    Write-Step 'Preview only. Re-run with -Apply to install.'
}
else {
    Write-Step 'Complete. Next: ./patches/P4.9-Validate-OperationalRunCoordinator.ps1; dotnet restore; dotnet build'
}
