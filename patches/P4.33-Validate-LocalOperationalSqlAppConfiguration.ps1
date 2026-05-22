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
        throw ("Expected file not found: {0}" -f $Path)
    }

    $content = Get-Content -LiteralPath $Path -Raw
    if (-not $content.Contains($Text)) {
        throw ("Expected text not found in {0}: {1}" -f $Path, $Text)
    }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path

Assert-Contains `
    -Path (Join-Path $repoRoot "config-samples/appsettings.AdminApi.LocalOperationalSql.sample.json") `
    -Text "OperationalSql"

Assert-Contains `
    -Path (Join-Path $repoRoot "docs/development/P4.33-local-operational-sql-app-configuration.md") `
    -Text "Local Operational SQL App Configuration"

Assert-Contains `
    -Path (Join-Path $repoRoot "scripts/validation/P4.33-Test-LocalOperationalSqlConfiguration.ps1") `
    -Text "ConnectionStrings:OperationalSql"

Write-Host "[P4.33] Validation passed."
