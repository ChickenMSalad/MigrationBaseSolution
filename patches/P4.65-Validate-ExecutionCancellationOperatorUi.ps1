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

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$workspacePath = Join-Path $repoRoot "apps/migration-admin-ui/src/features/executionSessions/ExecutionSessionWorkspace.tsx"

Assert-Contains -Path $workspacePath -Text "cancelExecutionSession"
Assert-Contains -Path $workspacePath -Text "Cancel session"
Assert-Contains -Path $workspacePath -Text "terminalStatuses"
Assert-Contains -Path $workspacePath -Text "selectedIsTerminal"
Assert-Contains -Path $workspacePath -Text "disabled={selectedIsTerminal}"

Write-Host "[P4.65] Validation passed."
