param([string]$BaseUrl = "https://localhost:55436")
$ErrorActionPreference = "Stop"

Write-Host "Checking global operational failure system-pair metrics route..."

$endpointMap = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/system/endpoints" `
    -ContentType "application/json"

$route = "/api/operational/failures/system-pair-metrics"
$match = $endpointMap | Where-Object { $_.routePattern -eq $route }

if (-not $match) {
    throw "Missing route: $route"
}

Write-Host "Found route: $route"
