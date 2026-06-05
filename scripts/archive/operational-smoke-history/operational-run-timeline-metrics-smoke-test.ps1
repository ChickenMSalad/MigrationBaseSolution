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
Write-Host "GET $BaseUrl/api/operational/runs/$runId/timeline/metrics"

$response = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/$runId/timeline/metrics" `
    -ContentType "application/json"

Write-Host "TotalEventCount: $($response.totalEventCount)"
Write-Host "FirstEventAt: $($response.firstEventAt)"
Write-Host "LastEventAt: $($response.lastEventAt)"

if ($response.eventTypes) {
    Write-Host ""
    Write-Host "Event type counts:"
    foreach ($item in $response.eventTypes) {
        Write-Host "- $($item.eventType): $($item.count)"
    }
}

if ($response.sources) {
    Write-Host ""
    Write-Host "Source counts:"
    foreach ($item in $response.sources) {
        Write-Host "- $($item.source): $($item.count)"
    }
}

$response | ConvertTo-Json -Depth 20
