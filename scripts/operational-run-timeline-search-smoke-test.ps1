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
Write-Host "Searching timeline for 'WorkItem'..."
$workItemResults = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/$runId/timeline/search?q=WorkItem&limit=10" `
    -ContentType "application/json"

$workItemResults | ConvertTo-Json -Depth 20

if ($workItemResults.eventCount -gt 10) {
    throw "Timeline search limit was not respected."
}

Write-Host ""
Write-Host "Searching timeline for 'Checkpoint'..."
$checkpointResults = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/$runId/timeline/search?q=Checkpoint&limit=10" `
    -ContentType "application/json"

$checkpointResults | ConvertTo-Json -Depth 20

Write-Host ""
Write-Host "Operational run timeline search smoke passed."
