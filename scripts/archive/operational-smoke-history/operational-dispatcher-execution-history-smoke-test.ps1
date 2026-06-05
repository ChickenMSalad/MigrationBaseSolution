param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

Write-Host "Dispatcher execution history:"
$executions = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/dispatcher/executions" `
    -ContentType "application/json"

$executions | ConvertTo-Json -Depth 10

if ($null -eq $executions) {
    exit 0
}

if ($executions.Count -gt 0) {
    $latest = $executions[0]

    Write-Host ""
    Write-Host "Latest execution:"
    Write-Host "ExecutionId: $($latest.executionId)"
    Write-Host "Outcome: $($latest.outcome)"
    Write-Host "LeasedCount: $($latest.leasedCount)"
    Write-Host "CompletedCount: $($latest.completedCount)"
    Write-Host "FailedCount: $($latest.failedCount)"

    Write-Host ""
    Write-Host "Execution detail:"

    Invoke-RestMethod `
        -Method Get `
        -Uri "$BaseUrl/api/operational/dispatcher/executions/$($latest.executionId)" `
        -ContentType "application/json" |
        ConvertTo-Json -Depth 10
}
