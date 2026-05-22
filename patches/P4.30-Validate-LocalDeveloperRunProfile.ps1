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
    -Path (Join-Path $repoRoot "apps/migration-admin-ui/.env.local.example") `
    -Text "VITE_ADMIN_API_BASE_URL=https://localhost:55436"

Assert-Contains `
    -Path (Join-Path $repoRoot "scripts/dev/Start-MigrationAdminLocal.ps1") `
    -Text "npm run dev -- --host localhost --port"

Assert-Contains `
    -Path (Join-Path $repoRoot "scripts/dev/Test-MigrationAdminLocalProfile.ps1") `
    -Text "/api/operational/command-center/summary"

Assert-Contains `
    -Path (Join-Path $repoRoot "docs/development/P4.30-local-developer-run-profile.md") `
    -Text "http://localhost:5174"

Write-Host "[P4.30] Validation passed."
