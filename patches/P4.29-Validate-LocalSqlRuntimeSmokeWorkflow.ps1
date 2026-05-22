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
    -Path (Join-Path $repoRoot "config-samples/appsettings.LocalSqlRuntimeSmoke.sample.json") `
    -Text "OperationalSql"

Assert-Contains `
    -Path (Join-Path $repoRoot "scripts/smoke/P4.29-Invoke-LocalSqlRuntimeSmoke.ps1") `
    -Text "/api/operational/command-center/summary"

Assert-Contains `
    -Path (Join-Path $repoRoot "scripts/smoke/P4.29-Test-OperationalSqlSchemaFiles.ps1") `
    -Text "MigrationWorkItems"

Assert-Contains `
    -Path (Join-Path $repoRoot "docs/operations/P4.29-local-sql-runtime-smoke-workflow.md") `
    -Text "Local SQL Runtime Smoke Workflow"

Write-Host "[P4.29] Validation passed."
