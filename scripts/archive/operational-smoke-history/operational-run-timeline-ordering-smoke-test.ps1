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

$timeline = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/$runId/timeline" `
    -ContentType "application/json"

$events = @($timeline.events)

for ($i = 1; $i -lt $events.Count; $i++) {
    $previous = [DateTimeOffset]$events[$i - 1].occurredAt
    $current = [DateTimeOffset]$events[$i].occurredAt

    if ($current -lt $previous) {
        throw "Timeline events are not sorted chronologically at index $i."
    }
}

Write-Host "Timeline contains $($events.Count) event(s)."
Write-Host "Timeline ordering smoke passed."
