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

Write-Host ""
Write-Host "Querying first 3 timeline events..."
$limited = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/$runId/timeline/query?limit=3" `
    -ContentType "application/json"

$limited | ConvertTo-Json -Depth 20

if ($limited.eventCount -gt 3) {
    throw "Timeline query limit was not respected."
}

Write-Host ""
Write-Host "Querying work-item events..."
$workItems = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/$runId/timeline/query?source=MigrationWorkItems&limit=10" `
    -ContentType "application/json"

$workItems | ConvertTo-Json -Depth 20

foreach ($event in @($workItems.events)) {
    if ($event.source -ne "MigrationWorkItems") {
        throw "Timeline source filter returned a non-work-item event."
    }
}

Write-Host ""
Write-Host "Operational run timeline query smoke passed."
