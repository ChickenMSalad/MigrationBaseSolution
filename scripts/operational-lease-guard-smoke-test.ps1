param(
    [string]$BaseUrl = "https://localhost:55436",
    [string]$WorkerId = "local-smoke-worker"
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
Write-Host "Current control state:"
Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/$runId/control-state" `
    -ContentType "application/json" | ConvertTo-Json -Depth 10

Write-Host ""
Write-Host "Attempting to lease one work item..."
$body = @{
    workerId = $WorkerId
    count = 1
} | ConvertTo-Json -Depth 5

Invoke-RestMethod `
    -Method Post `
    -Uri "$BaseUrl/api/operational/work-items/lease" `
    -ContentType "application/json" `
    -Body $body | ConvertTo-Json -Depth 10
