param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$PreviewLimit = 5
)

$ErrorActionPreference = "Stop"

$runs = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs" `
    -ContentType "application/json"

if ($runs.Count -eq 0) {
    throw "No operational runs found."
}

$runId = $runs[0].runId

Write-Host "Using run: $runId"

Write-Host ""
Write-Host "Loading run dashboard..."
$runDashboard = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/$runId/dashboard" `
    -ContentType "application/json"

Write-Host "Loading timeline metrics..."
$timelineMetrics = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/$runId/timeline/metrics" `
    -ContentType "application/json"

Write-Host "Loading timeline query preview..."
$timelinePreview = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/$runId/timeline/query?limit=$PreviewLimit" `
    -ContentType "application/json"

Write-Host "Loading timeline dashboard aggregate..."
$aggregate = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/$runId/timeline/dashboard?previewLimit=$PreviewLimit" `
    -ContentType "application/json"

Write-Host ""
Write-Host "Comparing aggregate with component endpoints..."

if ($aggregate.runId -ne $runId) {
    throw "Aggregate runId does not match selected runId."
}

if ($aggregate.runDashboard.runId -ne $runDashboard.runId) {
    throw "Aggregate run dashboard runId does not match run dashboard endpoint."
}

if ($aggregate.runDashboard.projection.runStatus -ne $runDashboard.projection.runStatus) {
    throw "Aggregate runStatus does not match run dashboard endpoint."
}

if ($aggregate.runDashboard.projection.projectionStatus -ne $runDashboard.projection.projectionStatus) {
    throw "Aggregate projectionStatus does not match run dashboard endpoint."
}

if ($aggregate.timelineMetrics.totalEventCount -ne $timelineMetrics.totalEventCount) {
    throw "Aggregate timeline totalEventCount does not match timeline metrics endpoint."
}

if ($aggregate.timelineMetrics.firstEventAt -ne $timelineMetrics.firstEventAt) {
    throw "Aggregate timeline firstEventAt does not match timeline metrics endpoint."
}

if ($aggregate.timelineMetrics.lastEventAt -ne $timelineMetrics.lastEventAt) {
    throw "Aggregate timeline lastEventAt does not match timeline metrics endpoint."
}

if ($aggregate.timelinePreview.eventCount -ne $timelinePreview.eventCount) {
    throw "Aggregate timeline preview eventCount does not match timeline query endpoint."
}

if ($aggregate.timelinePreview.eventCount -gt $PreviewLimit) {
    throw "Aggregate timeline preview limit was not respected."
}

Write-Host "RunStatus: $($aggregate.runDashboard.projection.runStatus)"
Write-Host "ProjectionStatus: $($aggregate.runDashboard.projection.projectionStatus)"
Write-Host "TimelineTotalEvents: $($aggregate.timelineMetrics.totalEventCount)"
Write-Host "TimelinePreviewEvents: $($aggregate.timelinePreview.eventCount)"

Write-Host ""
Write-Host "Operational run timeline dashboard consistency smoke passed."
