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
$storePath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Operational/Execution/SqlExecutionWorkItemQueueStore.cs"
$endpointPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Endpoints/Operational/Execution/ExecutionWorkItemQueueEndpointExtensions.cs"
$workerPath = Join-Path $repoRoot "scripts/workers/P4.57-Invoke-LocalExecutionWorker.ps1"

Assert-Contains -Path $storePath -Text "ExecutionWorkItemLeaseRenewed"
Assert-Contains -Path $storePath -Text "ExecutionWorkItemsRequeued"
Assert-Contains -Path $endpointPath -Text 'group.MapPost("/renew"'
Assert-Contains -Path $endpointPath -Text 'group.MapPost("/requeue"'
Assert-Contains -Path $workerPath -Text "RenewLeaseBeforeCompletion"

Write-Host "[P4.60] Validation passed."
