[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Assert-Contains {
    param([string]$Path, [string]$Text)
    if (-not (Test-Path -LiteralPath $Path)) { throw ("Expected file not found: {0}" -f $Path) }
    $content = Get-Content -LiteralPath $Path -Raw
    if (-not $content.Contains($Text)) { throw ("Expected text not found in {0}: {1}" -f $Path, $Text) }
}

function Assert-NotContains {
    param([string]$Path, [string]$Text)
    if (-not (Test-Path -LiteralPath $Path)) { throw ("Expected file not found: {0}" -f $Path) }
    $content = Get-Content -LiteralPath $Path -Raw
    if ($content.Contains($Text)) { throw ("Unexpected text found in {0}: {1}" -f $Path, $Text) }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$path = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Operational/Execution/SqlExecutionReplayPolicyService.cs"

Assert-Contains -Path $path -Text "ReadReplayDepthAsync"
Assert-Contains -Path $path -Text "SELECT ReplayDepth"
Assert-NotContains -Path $path -Text "bundle.Session?.ReplayDepth"

Write-Host "[P4.73-HOTFIX] Validation passed."
