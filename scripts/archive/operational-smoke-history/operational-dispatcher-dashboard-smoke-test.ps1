param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

Write-Host "Requesting operational dispatcher dashboard..."
Write-Host "GET $BaseUrl/api/operational/dispatcher/dashboard"

$response = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/dispatcher/dashboard" `
    -ContentType "application/json"

Write-Host "DispatcherEnabled: $($response.dispatcher.enabled)"
Write-Host "DispatcherMode: $($response.dispatcher.mode)"
Write-Host "EligibleWorkItemCount: $($response.diagnostics.eligibleWorkItemCount)"
Write-Host "TotalExecutionCount: $($response.executionMetrics.totalExecutionCount)"
Write-Host "RetentionMode: $($response.retention.mode)"
Write-Host "ExecutionHistoryReady: $($response.executionHistoryReadiness.ready)"

if ($response.messages) {
    Write-Host ""
    Write-Host "Messages:"
    foreach ($message in $response.messages) {
        Write-Host "- $message"
    }
}

$response | ConvertTo-Json -Depth 20
