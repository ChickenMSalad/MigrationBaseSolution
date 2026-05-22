[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Assert-Contains {
    param(
        [string]$Path,
        [string]$Text
    )

    $content = Get-Content -LiteralPath $Path -Raw

    if (-not $content.Contains($Text)) {
        throw ("Expected text not found in {0}: {1}" -f $Path, $Text)
    }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path

Assert-Contains `
    -Path (Join-Path $repoRoot "src/Core/Migration.Admin.Api/Program.cs") `
    -Text "app.MapOperationalCapacityEndpoints();"

Assert-Contains `
    -Path (Join-Path $repoRoot "apps/migration-admin-ui/src/App.tsx") `
    -Text "<CapacityForecastWorkspace />"

Write-Host "[P4.24] Validation passed."
