param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

Write-Host "=== P3 Operational Health Snapshot ==="

Write-Host ""
Write-Host "Mirror enablement guard:"
Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/mirror/enablement-guard" `
    -ContentType "application/json" |
    ConvertTo-Json -Depth 10

Write-Host ""
Write-Host "Operational diagnostics summary:"
Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/diagnostics/summary" `
    -ContentType "application/json" |
    ConvertTo-Json -Depth 20

Write-Host ""
Write-Host "Dispatcher dashboard:"
Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/dispatcher/dashboard" `
    -ContentType "application/json" |
    ConvertTo-Json -Depth 20

Write-Host ""
Write-Host "Global activity dashboard:"
Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/activity/dashboard?recentLimit=10&metricsSampleLimit=100" `
    -ContentType "application/json" |
    ConvertTo-Json -Depth 25

Write-Host ""
Write-Host "Recent failures:"
Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/recent?limit=10" `
    -ContentType "application/json" |
    ConvertTo-Json -Depth 20

Write-Host ""
Write-Host "P3 operational health snapshot completed."
