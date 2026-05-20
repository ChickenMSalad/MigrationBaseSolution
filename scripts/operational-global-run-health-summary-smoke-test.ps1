param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

Write-Host "Requesting global operational run health summary..."
Write-Host "GET $BaseUrl/api/operational/runs/health-summary"

$response = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/health-summary" `
    -ContentType "application/json"

Write-Host "TotalRunCount: $($response.totalRunCount)"
Write-Host "ActiveRunCount: $($response.activeRunCount)"
Write-Host "CompletedRunCount: $($response.completedRunCount)"
Write-Host "FailedRunCount: $($response.failedRunCount)"
Write-Host "TotalWorkItemCount: $($response.totalWorkItemCount)"
Write-Host "CompletedWorkItemCount: $($response.completedWorkItemCount)"
Write-Host "TotalFailureCount: $($response.totalFailureCount)"
Write-Host "CompletionPercent: $($response.completionPercent)"
Write-Host "GeneratedAt: $($response.generatedAt)"

if ($response.totalRunCount -lt 0) {
    throw "TotalRunCount cannot be negative."
}

if ($response.totalWorkItemCount -lt 0) {
    throw "TotalWorkItemCount cannot be negative."
}

if ($response.completionPercent -lt 0 -or $response.completionPercent -gt 100) {
    throw "CompletionPercent must be between 0 and 100."
}

if ($response.messages) {
    Write-Host ""
    Write-Host "Messages:"
    foreach ($message in $response.messages) {
        Write-Host "- $message"
    }
}

$response | ConvertTo-Json -Depth 20
