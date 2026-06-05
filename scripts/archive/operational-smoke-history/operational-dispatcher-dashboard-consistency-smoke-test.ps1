param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

Write-Host "Loading dispatcher status..."
$status = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/dispatcher/status" `
    -ContentType "application/json"

Write-Host "Loading dispatcher diagnostics..."
$diagnostics = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/dispatcher/diagnostics" `
    -ContentType "application/json"

Write-Host "Loading dispatcher execution metrics..."
$metrics = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/dispatcher/executions/metrics" `
    -ContentType "application/json"

Write-Host "Loading dispatcher dashboard..."
$dashboard = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/dispatcher/dashboard" `
    -ContentType "application/json"

Write-Host ""
Write-Host "Comparing dispatcher dashboard with source endpoints..."

if ($dashboard.dispatcher.enabled -ne $status.enabled) {
    throw "Dispatcher dashboard enabled value does not match status endpoint."
}

if ($dashboard.dispatcher.workerId -ne $status.workerId) {
    throw "Dispatcher dashboard workerId does not match status endpoint."
}

if ($dashboard.diagnostics.eligibleWorkItemCount -ne $diagnostics.eligibleWorkItemCount) {
    throw "Dispatcher dashboard eligibleWorkItemCount does not match diagnostics endpoint."
}

if ($dashboard.executionMetrics.totalExecutionCount -ne $metrics.totalExecutionCount) {
    throw "Dispatcher dashboard totalExecutionCount does not match metrics endpoint."
}

Write-Host "DispatcherMode: $($dashboard.dispatcher.mode)"
Write-Host "WorkerId: $($dashboard.dispatcher.workerId)"
Write-Host "EligibleWorkItemCount: $($dashboard.diagnostics.eligibleWorkItemCount)"
Write-Host "TotalExecutionCount: $($dashboard.executionMetrics.totalExecutionCount)"

Write-Host ""
Write-Host "Operational dispatcher dashboard consistency smoke passed."
