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
$apiPath = Join-Path $repoRoot "apps/migration-admin-ui/src/features/executionSessions/executionWorkItemApi.ts"
$typesPath = Join-Path $repoRoot "apps/migration-admin-ui/src/features/executionSessions/executionWorkItemTypes.ts"

Assert-Contains -Path $workspacePath -Text "Expand work queue"
Assert-Contains -Path $workspacePath -Text "Lease work"
Assert-Contains -Path $workspacePath -Text "Execution work items"
Assert-Contains -Path $apiPath -Text "/api/operational/execution-work-items/expand"
Assert-Contains -Path $apiPath -Text "/api/operational/execution-work-items/lease"
Assert-Contains -Path $typesPath -Text "ExecutionWorkItemQueueSummary"

Write-Host "[P4.56] Validation passed."
