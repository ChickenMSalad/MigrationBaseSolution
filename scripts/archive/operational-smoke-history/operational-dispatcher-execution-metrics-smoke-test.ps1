param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

Write-Host "Requesting dispatcher execution history metrics..."
Write-Host "GET $BaseUrl/api/operational/dispatcher/executions/metrics"

$response = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/dispatcher/executions/metrics" `
    -ContentType "application/json"

Write-Host "TotalExecutionCount: $($response.totalExecutionCount)"
Write-Host "CompletedExecutionCount: $($response.completedExecutionCount)"
Write-Host "CompletedWithFailuresExecutionCount: $($response.completedWithFailuresExecutionCount)"
Write-Host "FailedExecutionCount: $($response.failedExecutionCount)"
Write-Host "TotalLeasedCount: $($response.totalLeasedCount)"
Write-Host "TotalCompletedCount: $($response.totalCompletedCount)"
Write-Host "TotalFailedCount: $($response.totalFailedCount)"
Write-Host "AverageDurationMilliseconds: $($response.averageDurationMilliseconds)"

$response | ConvertTo-Json -Depth 10
