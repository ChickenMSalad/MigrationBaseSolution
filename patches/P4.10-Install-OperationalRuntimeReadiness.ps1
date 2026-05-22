[CmdletBinding()]
param(
    [switch]$WhatIf,
    [switch]$Apply
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($WhatIf -and $Apply) {
    throw 'Use either -WhatIf or -Apply, not both.'
}

function Write-Step {
    param([Parameter(Mandatory=$true)][string]$Message)
    Write-Host "[P4.10] $Message" -ForegroundColor Cyan
}

function Add-LineAfterBuilderCreation {
    param(
        [Parameter(Mandatory=$true)][string]$Content,
        [Parameter(Mandatory=$true)][string]$Line
    )

    if ($Content.Contains($Line)) {
        return $Content
    }

    $pattern = 'var\s+builder\s*=\s*WebApplication\.CreateBuilder\(args\)\s*;'
    if ($Content -notmatch $pattern) {
        throw "Could not find WebApplication builder creation in Program.cs. Required line: $Line"
    }

    return [regex]::Replace($Content, $pattern, ('$0' + [Environment]::NewLine + $Line), 1)
}

function Add-LineBeforeAppRun {
    param(
        [Parameter(Mandatory=$true)][string]$Content,
        [Parameter(Mandatory=$true)][string]$Line
    )

    if ($Content.Contains($Line)) {
        return $Content
    }

    $pattern = 'app\.Run\s*\('
    if ($Content -notmatch $pattern) {
        throw "Could not find app.Run(...) in Program.cs. Required line: $Line"
    }

    return [regex]::Replace($Content, $pattern, ($Line + [Environment]::NewLine + 'app.Run('), 1)
}

function Copy-PayloadTree {
    param(
        [Parameter(Mandatory=$true)][string]$SourceRoot,
        [Parameter(Mandatory=$true)][string]$DestinationRoot
    )

    if (-not (Test-Path -LiteralPath $SourceRoot)) {
        throw "Payload source not found: $SourceRoot"
    }

    $files = @(Get-ChildItem -LiteralPath $SourceRoot -Recurse -File)
    foreach ($file in $files) {
        $relative = $file.FullName.Substring($SourceRoot.Length).TrimStart('\','/')
        $destination = Join-Path $DestinationRoot $relative
        $destinationDirectory = Split-Path -Parent $destination

        if (-not $Apply) {
            Write-Step "WOULD copy $($file.FullName) -> $destination"
            continue
        }

        if (-not (Test-Path -LiteralPath $destinationDirectory)) {
            New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
        }

        Copy-Item -LiteralPath $file.FullName -Destination $destination -Force
        Write-Step "Copied $relative"
    }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$payloadRoot = Join-Path $repoRoot 'payload'
$applicationRoot = Join-Path $repoRoot 'src\Core\Migration.Application'
$sqlInfrastructureRoot = Join-Path $repoRoot 'src\Core\Migration.Infrastructure.Sql'
$adminApiRoot = Join-Path $repoRoot 'src\Core\Migration.Admin.Api'
$programPath = Join-Path $adminApiRoot 'Program.cs'

Write-Step "Repo root: $repoRoot"

$requiredPaths = @(
    (Join-Path $applicationRoot 'Migration.Application.csproj'),
    (Join-Path $sqlInfrastructureRoot 'Migration.Infrastructure.Sql.csproj'),
    (Join-Path $adminApiRoot 'Migration.Admin.Api.csproj'),
    $programPath
)

foreach ($path in $requiredPaths) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required path not found: $path"
    }
}

Copy-PayloadTree -SourceRoot (Join-Path $payloadRoot 'src\Core\Migration.Application') -DestinationRoot $applicationRoot
Copy-PayloadTree -SourceRoot (Join-Path $payloadRoot 'src\Core\Migration.Infrastructure.Sql') -DestinationRoot $sqlInfrastructureRoot
Copy-PayloadTree -SourceRoot (Join-Path $payloadRoot 'src\Core\Migration.Admin.Api') -DestinationRoot $adminApiRoot
Copy-PayloadTree -SourceRoot (Join-Path $payloadRoot 'docs') -DestinationRoot (Join-Path $repoRoot 'docs')

if (-not $Apply) {
    Write-Step 'WOULD update Program.cs with runtime readiness service and endpoint registration if missing'
}
else {
    $program = Get-Content -LiteralPath $programPath -Raw
    $program = Add-LineAfterBuilderCreation -Content $program -Line 'builder.Services.AddAdminApiOperationalRuntimeReadiness(builder.Configuration);'
    $program = Add-LineBeforeAppRun -Content $program -Line 'app.MapSqlOperationalRuntimeReadinessEndpoints();'
    Set-Content -LiteralPath $programPath -Value $program -Encoding UTF8
    Write-Step 'Updated Program.cs registration'
}

Write-Step 'Complete. Next: ./patches/P4.10-Validate-OperationalRuntimeReadiness.ps1; dotnet restore; dotnet build'
