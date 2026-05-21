param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$MetricsSampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "Requesting dispatcher pressure analytics..."
$response = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/dispatcher/pressure?metricsSampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

Write-Host "PressureScore: $($response.pressureScore)"
Write-Host "PressureLevel: $($response.pressureLevel)"
Write-Host "PressureReason: $($response.pressureReason)"
Write-Host "OutstandingWorkItemCount: $($response.outstandingWorkItemCount)"
Write-Host "LockedWorkItemCount: $($response.lockedWorkItemCount)"
Write-Host "FailedWorkItemCount: $($response.failedWorkItemCount)"
Write-Host "GeneratedAt: $($response.generatedAt)"

if ($response.pressureScore -lt 0 -or $response.pressureScore -gt 100) {
    throw "PressureScore must be between 0 and 100."
}

if (-not $response.pressureLevel) {
    throw "PressureLevel is required."
}

if (-not $response.pressureReason) {
    throw "PressureReason is required."
}

if (@($response.signals).Count -lt 1) {
    throw "Dispatcher pressure should include signals."
}

$response | ConvertTo-Json -Depth 25
