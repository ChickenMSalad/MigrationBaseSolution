param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

Write-Host "Checking operational route map..."
./scripts/admin-api-endpoint-map-smoke-test.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Checking operational run control state..."
$runs = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs" `
    -ContentType "application/json"

if ($runs.Count -eq 0) {
    Write-Host "No operational runs found."
    exit 0
}

$runId = $runs[0].runId

Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/$runId/control-state" `
    -ContentType "application/json" | ConvertTo-Json -Depth 10
