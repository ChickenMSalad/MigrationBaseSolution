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

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path

Assert-Contains `
    -Path (Join-Path $repoRoot "src/Core/Migration.Admin.Api/Program.cs") `
    -Text "app.MapOperationalCostAnalyticsEndpoints();"

Assert-Contains `
    -Path (Join-Path $repoRoot "apps/migration-admin-ui/src/App.tsx") `
    -Text "<CostAnalyticsWorkspace />"

Write-Host "[P4.25] Validation passed."
