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
    -Path (Join-Path $repoRoot "scripts/sql/P4.32-Initialize-OperationalSqlDatabase.ps1") `
    -Text "CREATE DATABASE"

Assert-Contains `
    -Path (Join-Path $repoRoot "scripts/sql/P4.32-Test-OperationalSqlDatabase.ps1") `
    -Text "MigrationWorkItems"

Assert-Contains `
    -Path (Join-Path $repoRoot "docs/operations/P4.32-operational-sql-database-bootstrap.md") `
    -Text "Operational SQL Database Bootstrap"

Write-Host "[P4.32] Validation passed."
