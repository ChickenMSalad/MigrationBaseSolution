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
Write-Host "Before resume:"
Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/$runId/control-state" `
    -ContentType "application/json" | ConvertTo-Json -Depth 10

$body = @{
    reason = "Local smoke resume request."
    requestedBy = "local-smoke"
} | ConvertTo-Json -Depth 5

Write-Host ""
Write-Host "Resume request:"
Invoke-RestMethod `
    -Method Post `
    -Uri "$BaseUrl/api/operational/runs/$runId/resume" `
    -ContentType "application/json" `
    -Body $body | ConvertTo-Json -Depth 10

Write-Host ""
Write-Host "Lease guard after resume:"
./scripts/operational-lease-guard-smoke-test.ps1 `
    -BaseUrl $BaseUrl
