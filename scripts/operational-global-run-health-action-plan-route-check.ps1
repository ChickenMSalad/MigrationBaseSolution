param([string]$BaseUrl = "https://localhost:55436")
$ErrorActionPreference = "Stop"

Write-Host "Checking global operational run health action plan route..."

$endpointMap = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/system/endpoints" `
    -ContentType "application/json"

$route = "/api/operational/runs/health-action-plan"
$match = $endpointMap | Where-Object { $_.routePattern -eq $route }

if (-not $match) {
    throw "Missing route: $route"
}

Write-Host "Found route: $route"
