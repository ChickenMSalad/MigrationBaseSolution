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

function Assert-NotContains {
    param(
        [string]$Path,
        [string]$Text
    )

    $content = Get-Content -LiteralPath $Path -Raw

    if ($content.Contains($Text)) {
        throw ("Unexpected legacy mapping still present in {0}: {1}" -f $Path, $Text)
    }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$programPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Program.cs"

Assert-Contains `
    -Path $programPath `
    -Text "app.MapMigrationOperationalEndpoints();"

Assert-NotContains `
    -Path $programPath `
    -Text "app.MapOperationalAuditTrailEndpoints();"

Assert-NotContains `
    -Path $programPath `
    -Text "app.MapOperationalNotificationEndpoints();"

Write-Host "[P4.26] Validation passed."
