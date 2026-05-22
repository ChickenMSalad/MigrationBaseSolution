[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Assert-File {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Expected file not found: $Path"
    }
}

function Assert-Contains {
    param(
        [string]$Path,
        [string]$Text
    )

    Assert-File $Path

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

    Assert-File $Path

    $content = Get-Content -LiteralPath $Path -Raw
    $count = ([regex]::Matches($content, [regex]::Escape($Text))).Count

    if ($count -ne 1) {
        throw ("Expected text to occur once in {0}; found {1}: {2}" -f $Path, $count, $Text)
    }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path

$endpointPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Endpoints/Operational/Audit/OperationalAuditTrailEndpointExtensions.cs"
$appPath = Join-Path $repoRoot "apps/migration-admin-ui/src/App.tsx"
$programPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Program.cs"

Assert-Contains $endpointPath "MapOperationalAuditTrailEndpoints"
Assert-OccursOnce $programPath "using Migration.Admin.Api.Endpoints.Operational.Audit;"
Assert-OccursOnce $programPath "app.MapOperationalAuditTrailEndpoints();"
Assert-OccursOnce $appPath "import { AuditTrailWorkspace } from './features/audit/AuditTrailWorkspace';"
Assert-OccursOnce $appPath "<AuditTrailWorkspace />"

Write-Host "[P4.21] Validation passed."
