param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

Write-Host "=== P3 Operational API Surface Audit ==="
Write-Host "GET $BaseUrl/api/system/endpoints"

$endpointMap = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/system/endpoints" `
    -ContentType "application/json"

$expectedRoutes = @(
    "/api/operational/mirror/enablement-guard",
    "/api/operational/mirror/last-invocation",
    "/api/operational/mirror/readiness",
    "/api/operational/mirror/status",
    "/api/operational/mirror/write-verification",
    "/api/operational/sql/schema/smoke-test",

    "/api/operational/runs",
    "/api/operational/runs/{runId:guid}",
    "/api/operational/runs/status-projections",
    "/api/operational/runs/{runId:guid}/status-projection",
    "/api/operational/runs/{runId:guid}/control-state",
    "/api/operational/runs/{runId:guid}/cancel",
    "/api/operational/runs/{runId:guid}/abort",
    "/api/operational/runs/{runId:guid}/resume",
    "/api/operational/runs/{runId:guid}/completion-readiness",
    "/api/operational/runs/{runId:guid}/finalize-completion",
    "/api/operational/runs/{runId:guid}/failure-readiness",
    "/api/operational/runs/{runId:guid}/finalize-failure",
    "/api/operational/runs/{runId:guid}/status-reconciliation",
    "/api/operational/runs/{runId:guid}/reconcile-status",
    "/api/operational/runs/{runId:guid}/dashboard",

    "/api/operational/runs/{runId:guid}/timeline",
    "/api/operational/runs/{runId:guid}/timeline/query",
    "/api/operational/runs/{runId:guid}/timeline/metrics",
    "/api/operational/runs/{runId:guid}/timeline/dashboard",
    "/api/operational/runs/{runId:guid}/timeline/search",
    "/api/operational/runs/{runId:guid}/timeline/catalog",
    "/api/operational/runs/timeline/catalog",

    "/api/operational/work-items/lease",
    "/api/operational/work-items/{workItemId:guid}/heartbeat",
    "/api/operational/work-items/{workItemId:guid}/complete",
    "/api/operational/work-items/{workItemId:guid}/fail",
    "/api/operational/work-items/{workItemId:guid}/release",
    "/api/operational/work-items/{workItemId:guid}/reset",
    "/api/operational/work-items/expired-leases",
    "/api/operational/dispatcher/status",
    "/api/operational/dispatcher/run-once",
    "/api/operational/dispatcher/diagnostics",
    "/api/operational/dispatcher/executions",
    "/api/operational/dispatcher/executions/{executionId:guid}",
    "/api/operational/dispatcher/executions/readiness",
    "/api/operational/dispatcher/executions/metrics",
    "/api/operational/dispatcher/executions/query",
    "/api/operational/dispatcher/executions/retention/status",
    "/api/operational/dispatcher/executions/retention/purge-eligible",
    "/api/operational/dispatcher/dashboard",

    "/api/operational/activity/recent",
    "/api/operational/activity/query",
    "/api/operational/activity/metrics",
    "/api/operational/activity/dashboard",

    "/api/operational/failures/recent",

    "/api/operational/metrics/work-items",
    "/api/operational/metrics/leases",
    "/api/operational/metrics/runs",
    "/api/operational/diagnostics/summary"
)

$missing = New-Object System.Collections.Generic.List[string]
$found = New-Object System.Collections.Generic.List[string]

foreach ($route in $expectedRoutes) {
    $match = $endpointMap | Where-Object { $_.routePattern -eq $route }

    if ($match) {
        $found.Add($route)
    }
    else {
        $missing.Add($route)
    }
}

Write-Host ""
Write-Host "Expected operational route count: $($expectedRoutes.Count)"
Write-Host "Found: $($found.Count)"
Write-Host "Missing: $($missing.Count)"

if ($found.Count -gt 0) {
    Write-Host ""
    Write-Host "Found routes:"
    foreach ($route in $found) {
        Write-Host " + $route"
    }
}

if ($missing.Count -gt 0) {
    Write-Host ""
    Write-Host "Missing routes:"
    foreach ($route in $missing) {
        Write-Host " - $route"
    }

    throw "Operational API surface audit failed. Missing route count: $($missing.Count)"
}

Write-Host ""
Write-Host "Operational API surface audit passed."
