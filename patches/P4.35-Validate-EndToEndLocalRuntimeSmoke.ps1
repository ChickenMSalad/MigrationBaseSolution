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
    -Path (Join-Path $repoRoot "scripts/smoke/P4.35-Invoke-EndToEndLocalRuntimeSmoke.ps1") `
    -Text "/api/operational/sql/health"

Assert-Contains `
    -Path (Join-Path $repoRoot "scripts/smoke/P4.35-Invoke-EndToEndLocalRuntimeSmoke.ps1") `
    -Text "npm run build"

Assert-Contains `
    -Path (Join-Path $repoRoot "docs/operations/P4.35-end-to-end-local-runtime-smoke.md") `
    -Text "End-to-End Local Runtime Smoke Orchestrator"

Write-Host "[P4.35] Validation passed."
