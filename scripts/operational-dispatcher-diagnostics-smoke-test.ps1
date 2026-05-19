param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

Write-Host "Operational dispatcher status:"
Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/dispatcher/status" `
    -ContentType "application/json" | ConvertTo-Json -Depth 10

Write-Host ""
Write-Host "Operational dispatcher diagnostics:"
Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/dispatcher/diagnostics" `
    -ContentType "application/json" | ConvertTo-Json -Depth 10
