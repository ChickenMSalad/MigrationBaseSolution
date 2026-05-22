[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [switch]$Apply
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host ("[P4.49] {0}" -f $Message)
}

function Copy-PayloadFile {
    param([string]$RelativePath)

    $source = Join-Path $payloadRoot $RelativePath
    $target = Join-Path $repoRoot $RelativePath

    if (-not (Test-Path -LiteralPath $source)) {
        throw ("Payload file not found: {0}" -f $source)
    }

    if (-not $Apply) {
        Write-Step ("WOULD copy {0}" -f $RelativePath)
        return
    }

    $directory = Split-Path -Parent $target
    if (-not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    Copy-Item -LiteralPath $source -Destination $target -Force
    Write-Step ("Copied {0}" -f $RelativePath)
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$payloadRoot = Join-Path $repoRoot "payload"

Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Events/OperationalEventQueryRequest.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Events/OperationalEventAggregateSummary.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Events/IOperationalEventQueryService.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Events/SqlOperationalEventQueryService.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Endpoints/Operational/Events/OperationalEventQueryEndpointExtensions.cs"
Copy-PayloadFile "apps/migration-admin-ui/src/features/events/operationalEventTimelineTypes.ts"
Copy-PayloadFile "apps/migration-admin-ui/src/features/events/operationalEventTimelineApi.ts"
Copy-PayloadFile "apps/migration-admin-ui/src/features/events/operationalEventExportApi.ts"
Copy-PayloadFile "apps/migration-admin-ui/src/features/events/OperationalEventTimelineWorkspace.tsx"
Copy-PayloadFile "docs/operations/P4.49-operational-event-time-window-aggregates.md"

Write-Step "Complete."
