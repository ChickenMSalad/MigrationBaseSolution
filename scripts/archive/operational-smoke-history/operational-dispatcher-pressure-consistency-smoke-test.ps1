param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$MetricsSampleLimit = 100
)

$ErrorActionPreference = "Stop"

$queue = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/queue/depth" `
    -ContentType "application/json"

$pressure = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/dispatcher/pressure?metricsSampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

if ($pressure.queueDepth.totalWorkItemCount -ne $queue.totalWorkItemCount) {
    throw "Pressure queueDepth totalWorkItemCount does not match queue depth endpoint."
}

if ($pressure.outstandingWorkItemCount -ne $queue.outstandingWorkItemCount) {
    throw "Pressure outstandingWorkItemCount does not match queue depth endpoint."
}

if ($pressure.lockedWorkItemCount -ne $queue.lockedWorkItemCount) {
    throw "Pressure lockedWorkItemCount does not match queue depth endpoint."
}

if ($pressure.failedWorkItemCount -ne $queue.failedWorkItemCount) {
    throw "Pressure failedWorkItemCount does not match queue depth endpoint."
}

$expectedPressureLevel = if ($pressure.pressureScore -ge 75) {
    "Critical"
}
elseif ($pressure.pressureScore -ge 50) {
    "High"
}
elseif ($pressure.pressureScore -ge 25) {
    "Elevated"
}
else {
    "Normal"
}

if ($pressure.pressureLevel -ne $expectedPressureLevel) {
    throw "PressureLevel does not match pressure score thresholds."
}

Write-Host "PressureScore: $($pressure.pressureScore)"
Write-Host "PressureLevel: $($pressure.pressureLevel)"
Write-Host "QueueTotalWorkItemCount: $($pressure.queueDepth.totalWorkItemCount)"
Write-Host "Operational dispatcher pressure consistency smoke passed."
