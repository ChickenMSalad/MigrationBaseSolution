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
Write-Host "GET $BaseUrl/api/operational/runs/$runId/timeline/catalog"

$response = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/$runId/timeline/catalog" `
    -ContentType "application/json"

Write-Host "EventTypeCount: $($response.eventTypeCount)"
Write-Host "SourceCount: $($response.sourceCount)"

if ($response.eventTypes) {
    Write-Host ""
    Write-Host "Event types:"
    foreach ($eventType in $response.eventTypes) {
        Write-Host "- $eventType"
    }
}

if ($response.sources) {
    Write-Host ""
    Write-Host "Sources:"
    foreach ($source in $response.sources) {
        Write-Host "- $source"
    }
}

$response | ConvertTo-Json -Depth 10
