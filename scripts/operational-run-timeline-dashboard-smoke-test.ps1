param(
    [string]$BaseUrl = "https://localhost:55436"
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
Write-Host "GET $BaseUrl/api/operational/runs/$runId/timeline/dashboard?previewLimit=5"

$response = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/$runId/timeline/dashboard?previewLimit=5" `
    -ContentType "application/json"

Write-Host "RunStatus: $($response.runDashboard.projection.runStatus)"
Write-Host "ProjectionStatus: $($response.runDashboard.projection.projectionStatus)"
Write-Host "TotalTimelineEvents: $($response.timelineMetrics.totalEventCount)"
Write-Host "TimelinePreviewCount: $($response.timelinePreview.eventCount)"

if ($response.timelinePreview.eventCount -gt 5) {
    throw "Timeline preview limit was not respected."
}

if ($response.messages) {
    Write-Host ""
    Write-Host "Messages:"
    foreach ($message in $response.messages) {
        Write-Host "- $message"
    }
}

$response | ConvertTo-Json -Depth 25
