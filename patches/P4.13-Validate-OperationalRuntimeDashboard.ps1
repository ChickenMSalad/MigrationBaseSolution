[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Assert-Path {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw ("Expected path not found: {0}" -f $Path)
    }
}

function Assert-Text {
    param(
        [string]$Path,
        [string]$Text
    )

    Assert-Path -Path $Path

    $content = Get-Content -LiteralPath $Path -Raw

    if (-not $content.Contains($Text)) {
        throw ("Expected text not found in {0}: {1}" -f $Path, $Text)
    }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$uiRoot = Join-Path $repoRoot "apps\migration-admin-ui"

Assert-Path -Path (Join-Path $uiRoot "src\lib\operationalRuntimeApi.ts")
Assert-Path -Path (Join-Path $uiRoot "src\components\RuntimeStatusBadge.tsx")
Assert-Path -Path (Join-Path $uiRoot "src\components\OperationalRuntimeDashboard.tsx")
Assert-Path -Path (Join-Path $repoRoot "docs\ui\P4.13-operational-runtime-dashboard.md")

Assert-Text `
    -Path (Join-Path $uiRoot "src\lib\operationalRuntimeApi.ts") `
    -Text "getOperationalRuntimeDashboard"

Assert-Text `
    -Path (Join-Path $uiRoot "src\components\OperationalRuntimeDashboard.tsx") `
    -Text "OperationalRuntimeDashboard"

Write-Host "[P4.13] Validation passed."
