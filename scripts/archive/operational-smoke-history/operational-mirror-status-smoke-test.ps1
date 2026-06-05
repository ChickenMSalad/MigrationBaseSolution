param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

$url = "$BaseUrl/api/operational/mirror/status"

Write-Host "Requesting operational mirror status..."
Write-Host "GET $url"

$response = Invoke-RestMethod `
    -Method Get `
    -Uri $url `
    -ContentType "application/json"

Write-Host "Operational mirror enabled: $($response.enabled)"
Write-Host "Mirror service registered: $($response.mirrorServiceRegistered)"

$response | ConvertTo-Json -Depth 10
