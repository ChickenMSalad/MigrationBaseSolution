param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

Write-Host "Loading latest operational run..."

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
Write-Host "Loading run detail..."
$detail = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/$runId" `
    -ContentType "application/json"

Write-Host ""
Write-Host "Loading run timeline..."
$timeline = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/$runId/timeline" `
    -ContentType "application/json"

if ($timeline.runId -ne $runId) {
    throw "Timeline runId does not match selected runId."
}

if ($timeline.eventCount -ne @($timeline.events).Count) {
    throw "Timeline eventCount does not match actual events length."
}

if ($timeline.eventCount -lt 1) {
    throw "Timeline should include at least one run-created event."
}

$runCreated = @($timeline.events) | Where-Object { $_.eventType -eq "RunCreated" }

if (-not $runCreated) {
    throw "Timeline does not include RunCreated event."
}

if ($detail.runId -and $detail.runId -ne $timeline.runId) {
    throw "Run detail runId does not match timeline runId."
}

Write-Host "TimelineEventCount: $($timeline.eventCount)"
Write-Host "RunCreatedEvents: $(@($runCreated).Count)"

Write-Host ""
Write-Host "Timeline consistency smoke passed."
