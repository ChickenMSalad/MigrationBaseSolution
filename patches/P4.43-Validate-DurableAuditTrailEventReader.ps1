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
$endpointPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Endpoints/Operational/Audit/OperationalAuditTrailEndpointExtensions.cs"

Assert-Contains -Path $endpointPath -Text "IOperationalEventStore eventStore"
Assert-Contains -Path $endpointPath -Text "ReadRecentEventsSafelyAsync"
Assert-Contains -Path $endpointPath -Text "ToAuditTrailEvent"
Assert-Contains -Path $endpointPath -Text "CreateLiveFallbackEvents"

Write-Host "[P4.43] Validation passed."
