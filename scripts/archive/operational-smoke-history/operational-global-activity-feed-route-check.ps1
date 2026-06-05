param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

Write-Host "Checking global operational activity feed route..."
Write-Host "GET $BaseUrl/api/system/endpoints"

$endpointMap = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/system/endpoints" `
    -ContentType "application/json"

$route = "/api/operational/activity/recent"
$match = $endpointMap | Where-Object { $_.routePattern -eq $route }

if (-not $match) {
    throw "Missing route: $route"
}

Write-Host "Found route: $route"
Write-Host "Global operational activity feed route is mapped."
