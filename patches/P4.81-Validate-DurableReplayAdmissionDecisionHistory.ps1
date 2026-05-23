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
Assert-Contains -Path (Join-Path $repoRoot "database/sql/operational/012_create_execution_replay_admission_decisions.sql") -Text "MigrationExecutionReplayAdmissionDecisions"
Assert-Contains -Path (Join-Path $repoRoot "src/Core/Migration.Admin.Api/Operational/Execution/SqlExecutionReplayAdmissionService.cs") -Text "PersistDecisionAsync"
Assert-Contains -Path (Join-Path $repoRoot "src/Core/Migration.Admin.Api/Operational/Execution/SqlExecutionReplayAdmissionService.cs") -Text "ReadHistoryAsync"
Assert-Contains -Path (Join-Path $repoRoot "src/Core/Migration.Admin.Api/Endpoints/Operational/Execution/ExecutionReplayAdmissionEndpointExtensions.cs") -Text "GetExecutionReplayAdmissionHistory"
Assert-Contains -Path (Join-Path $repoRoot "apps/migration-admin-ui/src/features/executionSessions/executionReplayAdmissionApi.ts") -Text "fetchExecutionReplayAdmissionHistory"

Write-Host "[P4.81] Validation passed."
