param([string]$BaseUrl = "https://localhost:55436")
$ErrorActionPreference = "Stop"

Write-Host "Requesting global operational queue depth analytics..."
Write-Host "GET $BaseUrl/api/operational/queue/depth"

$response = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/queue/depth" `
    -ContentType "application/json"

Write-Host "TotalWorkItemCount: $($response.totalWorkItemCount)"
Write-Host "OutstandingWorkItemCount: $($response.outstandingWorkItemCount)"
Write-Host "LockedWorkItemCount: $($response.lockedWorkItemCount)"
Write-Host "CompletedWorkItemCount: $($response.completedWorkItemCount)"
Write-Host "FailedWorkItemCount: $($response.failedWorkItemCount)"
Write-Host "CompletionPercent: $($response.completionPercent)"
Write-Host "QueuePressureScore: $($response.queuePressureScore)"
Write-Host "QueuePressureLevel: $($response.queuePressureLevel)"
Write-Host "GeneratedAt: $($response.generatedAt)"

if ($response.totalWorkItemCount -lt 0) {
    throw "TotalWorkItemCount cannot be negative."
}

if ($response.completionPercent -lt 0 -or $response.completionPercent -gt 100) {
    throw "CompletionPercent must be between 0 and 100."
}

if ($response.queuePressureScore -lt 0 -or $response.queuePressureScore -gt 100) {
    throw "QueuePressureScore must be between 0 and 100."
}

if (-not $response.queuePressureLevel) {
    throw "QueuePressureLevel is required."
}

$response | ConvertTo-Json -Depth 20
