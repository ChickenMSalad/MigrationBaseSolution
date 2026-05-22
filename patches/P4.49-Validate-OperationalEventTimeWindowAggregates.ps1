[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Assert-Contains {
    param([string]$Path, [string]$Text)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw ("Expected file not found: {0}" -f $Path)
    }

    $content = Get-Content -LiteralPath $Path -Raw

    if (-not $content.Contains($Text)) {
        throw ("Expected text not found in {0}: {1}" -f $Path, $Text)
    }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path

Assert-Contains `
    -Path (Join-Path $repoRoot "src/Core/Migration.Admin.Api/Operational/Events/SqlOperationalEventQueryService.cs") `
    -Text "GROUP BY"

Assert-Contains `
    -Path (Join-Path $repoRoot "src/Core/Migration.Admin.Api/Endpoints/Operational/Events/OperationalEventQueryEndpointExtensions.cs") `
    -Text "GetOperationalEventQuerySummary"

Assert-Contains `
    -Path (Join-Path $repoRoot "apps/migration-admin-ui/src/features/events/OperationalEventTimelineWorkspace.tsx") `
    -Text "type=""datetime-local"""

Assert-Contains `
    -Path (Join-Path $repoRoot "apps/migration-admin-ui/src/features/events/operationalEventTimelineTypes.ts") `
    -Text "OperationalEventAggregateSummary"

Write-Host "[P4.49] Validation passed."
