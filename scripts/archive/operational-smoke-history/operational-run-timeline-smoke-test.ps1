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
Write-Host "GET $BaseUrl/api/operational/runs/$runId/timeline"

$response = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/$runId/timeline" `
    -ContentType "application/json"

Write-Host "EventCount: $($response.eventCount)"

if ($response.events) {
    Write-Host ""
    Write-Host "Timeline preview:"
    $response.events |
        Select-Object -First 10 |
        ForEach-Object {
            Write-Host "$($_.occurredAt) [$($_.eventType)] $($_.message)"
        }
}

$response | ConvertTo-Json -Depth 20
