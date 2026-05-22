[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Assert-File {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw ("Expected file not found: {0}" -f $Path)
    }
}

function Assert-Contains {
    param(
        [string]$Path,
        [string]$Text
    )

    Assert-File -Path $Path

    $content = Get-Content -LiteralPath $Path -Raw
    if (-not $content.Contains($Text)) {
        throw ("Expected text not found in {0}: {1}" -f $Path, $Text)
    }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$schemaPath = Join-Path $repoRoot "database/sql/operational/001_create_operational_backbone.sql"

Assert-Contains -Path $schemaPath -Text "MigrationProjects"
Assert-Contains -Path $schemaPath -Text "MigrationRuns"
Assert-Contains -Path $schemaPath -Text "MigrationWorkItems"
Assert-Contains -Path $schemaPath -Text "MigrationFailures"

Write-Host "[P4.29-SCHEMA] Operational SQL schema file validation passed."
