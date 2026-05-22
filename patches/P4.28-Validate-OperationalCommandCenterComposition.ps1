[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Assert-Contains {
    param(
        [string]$Path,
        [string]$Text
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw ("File not found: {0}" -f $Path)
    }

    $content = Get-Content -LiteralPath $Path -Raw
    if (-not $content.Contains($Text)) {
        throw ("Expected text not found in {0}: {1}" -f $Path, $Text)
    }
}

function Assert-OccursOnce {
    param(
        [string]$Path,
        [string]$Text
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw ("File not found: {0}" -f $Path)
    }

    $content = Get-Content -LiteralPath $Path -Raw
    $count = ([regex]::Matches($content, [regex]::Escape($Text))).Count

    if ($count -ne 1) {
        throw ("Expected text to occur once in {0}; found {1}: {2}" -f $Path, $count, $Text)
    }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$endpointPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Endpoints/Operational/CommandCenter/OperationalCommandCenterEndpointExtensions.cs"
$compositionPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Endpoints/Operational/MigrationOperationalEndpointCompositionExtensions.cs"
$uiCompositionPath = Join-Path $repoRoot "apps/migration-admin-ui/src/features/operational/OperationalWorkspaceComposition.tsx"

Assert-Contains `
    -Path $endpointPath `
    -Text "MapOperationalCommandCenterEndpoints"

Assert-OccursOnce `
    -Path $compositionPath `
    -Text "using Migration.Admin.Api.Endpoints.Operational.CommandCenter;"

Assert-OccursOnce `
    -Path $compositionPath `
    -Text "endpoints.MapOperationalCommandCenterEndpoints();"

Assert-OccursOnce `
    -Path $uiCompositionPath `
    -Text "import { CommandCenterSummaryWorkspace } from '../commandCenter/CommandCenterSummaryWorkspace';"

Assert-OccursOnce `
    -Path $uiCompositionPath `
    -Text "<CommandCenterSummaryWorkspace />"

Write-Host "[P4.28] Validation passed."
