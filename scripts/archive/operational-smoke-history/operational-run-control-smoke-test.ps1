param(
    [string]$BaseUrl = "https://localhost:55436"
)

$runs = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs"

$runId = $runs[0].runId

Write-Host "Using run: $runId"

Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/$runId/control-state" | ConvertTo-Json -Depth 10

$body = @{
    reason = "Local smoke cancel request."
    requestedBy = "local-smoke"
} | ConvertTo-Json

Invoke-RestMethod `
    -Method Post `
    -Uri "$BaseUrl/api/operational/runs/$runId/cancel" `
    -ContentType "application/json" `
    -Body $body | ConvertTo-Json -Depth 10
