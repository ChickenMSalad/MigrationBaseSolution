param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

Write-Host "Loading global run health summary..."
$summary = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/health-summary" `
    -ContentType "application/json"

Write-Host "Loading status projections..."
$projections = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/status-projections" `
    -ContentType "application/json"

Write-Host "Loading recent failures..."
$failures = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/recent?limit=500" `
    -ContentType "application/json"

$projectionItems = @($projections)
$runStatusTotal = 0

foreach ($status in @($summary.runStatuses)) {
    $runStatusTotal += $status.count
}

if ($summary.totalRunCount -ne $runStatusTotal) {
    throw "Run health totalRunCount does not match runStatuses sum."
}

if ($projectionItems.Count -gt 0 -and $summary.totalRunCount -lt $projectionItems.Count) {
    throw "Run health totalRunCount is less than projection count."
}

$expectedCompleted = @($projectionItems | Where-Object { $_.runStatus -eq "Completed" }).Count
$expectedFailed = @($projectionItems | Where-Object { $_.runStatus -eq "Failed" }).Count

if ($projectionItems.Count -gt 0 -and $summary.completedRunCount -lt $expectedCompleted) {
    throw "Run health completedRunCount is less than completed projection count."
}

if ($projectionItems.Count -gt 0 -and $summary.failedRunCount -lt $expectedFailed) {
    throw "Run health failedRunCount is less than failed projection count."
}

if ($summary.totalFailureCount -lt @($failures.failures).Count) {
    throw "Run health totalFailureCount is less than recent failures sample count."
}

$workItemParts =
    $summary.outstandingWorkItemCount +
    $summary.lockedWorkItemCount +
    $summary.completedWorkItemCount +
    $summary.failedWorkItemCount

if ($workItemParts -gt $summary.totalWorkItemCount) {
    throw "Run health work-item bucket counts exceed totalWorkItemCount."
}

if ($summary.completionPercent -lt 0 -or $summary.completionPercent -gt 100) {
    throw "Run health completionPercent must be between 0 and 100."
}

Write-Host "TotalRunCount: $($summary.totalRunCount)"
Write-Host "ProjectionCount: $($projectionItems.Count)"
Write-Host "RunStatusTotal: $runStatusTotal"
Write-Host "TotalWorkItemCount: $($summary.totalWorkItemCount)"
Write-Host "WorkItemBucketSubtotal: $workItemParts"
Write-Host "TotalFailureCount: $($summary.totalFailureCount)"
Write-Host "RecentFailureSampleCount: $(@($failures.failures).Count)"
Write-Host "CompletionPercent: $($summary.completionPercent)"

Write-Host ""
Write-Host "Global operational run health summary consistency smoke passed."
