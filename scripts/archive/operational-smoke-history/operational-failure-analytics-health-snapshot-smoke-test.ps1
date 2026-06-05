param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$Limit = 10,
    [int]$SampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "=== Operational Failure Analytics Health Snapshot ==="

Write-Host ""
Write-Host "Recent failures:"
Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/recent?limit=$Limit" `
    -ContentType "application/json" |
    ConvertTo-Json -Depth 20

Write-Host ""
Write-Host "Failure metrics:"
Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/metrics?sampleLimit=$SampleLimit" `
    -ContentType "application/json" |
    ConvertTo-Json -Depth 20

Write-Host ""
Write-Host "Failure analytics dashboard:"
Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/analytics-dashboard?recentLimit=$Limit&metricsSampleLimit=$SampleLimit" `
    -ContentType "application/json" |
    ConvertTo-Json -Depth 30

Write-Host ""
Write-Host "Failure preset dashboard:"
Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/analytics-preset-dashboard?presetKey=all-recent&limit=$Limit" `
    -ContentType "application/json" |
    ConvertTo-Json -Depth 30

Write-Host ""
Write-Host "Failure favorites:"
Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/analytics-preset-favorites" `
    -ContentType "application/json" |
    ConvertTo-Json -Depth 20

Write-Host ""
Write-Host "Operational failure analytics health snapshot completed."
