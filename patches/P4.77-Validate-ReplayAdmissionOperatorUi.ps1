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
$apiPath = Join-Path $repoRoot "apps/migration-admin-ui/src/features/executionSessions/executionReplayAdmissionApi.ts"
$typesPath = Join-Path $repoRoot "apps/migration-admin-ui/src/features/executionSessions/executionReplayAdmissionTypes.ts"
$workspacePath = Join-Path $repoRoot "apps/migration-admin-ui/src/features/executionSessions/ExecutionSessionWorkspace.tsx"

Assert-Contains -Path $apiPath -Text "/api/operational/execution-replay/admission/evaluate"
Assert-Contains -Path $typesPath -Text "ExecutionReplayAdmissionEvaluationResult"
Assert-Contains -Path $workspacePath -Text "Evaluate admission"
Assert-Contains -Path $workspacePath -Text "Replay admission"
Assert-Contains -Path $workspacePath -Text "replayAdmissionTake"

Write-Host "[P4.77] Validation passed."
