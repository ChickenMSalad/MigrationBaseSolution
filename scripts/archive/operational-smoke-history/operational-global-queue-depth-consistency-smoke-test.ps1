param([string]$BaseUrl = "https://localhost:55436")
$ErrorActionPreference = "Stop"

Write-Host "Loading queue depth analytics..."
$queue = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/queue/depth" `
    -ContentType "application/json"

Write-Host "Loading run health summary..."
$health = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/health-summary" `
    -ContentType "application/json"

$statusTotal = 0
foreach ($status in @($queue.statuses)) {
    $statusTotal += $status.count
}

if ($queue.totalWorkItemCount -ne $statusTotal) {
    throw "Queue totalWorkItemCount does not match status bucket sum."
}

if ($queue.totalWorkItemCount -ne $health.totalWorkItemCount) {
    throw "Queue totalWorkItemCount does not match run health summary."
}

if ($queue.outstandingWorkItemCount -ne $health.outstandingWorkItemCount) {
    throw "Queue outstandingWorkItemCount does not match run health summary."
}

if ($queue.lockedWorkItemCount -ne $health.lockedWorkItemCount) {
    throw "Queue lockedWorkItemCount does not match run health summary."
}

if ($queue.completedWorkItemCount -ne $health.completedWorkItemCount) {
    throw "Queue completedWorkItemCount does not match run health summary."
}

if ($queue.failedWorkItemCount -ne $health.failedWorkItemCount) {
    throw "Queue failedWorkItemCount does not match run health summary."
}

$expectedPressureLevel = if ($queue.queuePressureScore -ge 75) {
    "Critical"
}
elseif ($queue.queuePressureScore -ge 50) {
    "High"
}
elseif ($queue.queuePressureScore -ge 25) {
    "Elevated"
}
else {
    "Normal"
}

if ($queue.queuePressureLevel -ne $expectedPressureLevel) {
    throw "QueuePressureLevel does not match QueuePressureScore thresholds."
}

Write-Host "TotalWorkItemCount: $($queue.totalWorkItemCount)"
Write-Host "StatusTotal: $statusTotal"
Write-Host "QueuePressureScore: $($queue.queuePressureScore)"
Write-Host "QueuePressureLevel: $($queue.queuePressureLevel)"
Write-Host ""
Write-Host "Global operational queue depth consistency smoke passed."
