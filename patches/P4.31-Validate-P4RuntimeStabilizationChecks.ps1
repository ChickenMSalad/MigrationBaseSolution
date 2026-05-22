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
    -Path (Join-Path $repoRoot "scripts/validation/P4.31-Test-P4RuntimeStabilization.ps1") `
    -Text "Inline PackageReference Version"

Assert-Contains `
    -Path (Join-Path $repoRoot "scripts/validation/P4.31-Test-P4RuntimeBuild.ps1") `
    -Text "npm run build"

Assert-Contains `
    -Path (Join-Path $repoRoot "docs/operations/P4.31-p4-runtime-stabilization-checks.md") `
    -Text "duplicate imports"

Write-Host "[P4.31] Validation passed."
