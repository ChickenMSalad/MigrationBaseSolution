[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Assert-Contains {
    param([string]$Path, [string]$Text)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw ("Expected file not found: {0}" -f $Path)
    }

    $content = Get-Content -LiteralPath $Path -Raw

    if (-not $content.Contains($Text)) {
        throw ("Expected text not found in {0}: {1}" -f $Path, $Text)
    }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$workspacePath = Join-Path $repoRoot "apps/migration-admin-ui/src/features/executionSessions/ExecutionSessionWorkspace.tsx"

Assert-Contains -Path $workspacePath -Text "forceAdmitExecutionReplay"
Assert-Contains -Path $workspacePath -Text "forceDeferExecutionReplay"
Assert-Contains -Path $workspacePath -Text "manualAdmissionOperator"
Assert-Contains -Path $workspacePath -Text "manualAdmissionReason"
Assert-Contains -Path $workspacePath -Text "Force admit replay"
Assert-Contains -Path $workspacePath -Text "Force defer replay"

Write-Host "[P4.85] Validation passed."
