param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

Write-Host "Checking operational dashboard routes..."
Write-Host "GET $BaseUrl/api/system/endpoints"

$endpointMap = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/system/endpoints" `
    -ContentType "application/json"

$expectedRoutes = @(
    "/api/operational/dispatcher/dashboard",
    "/api/operational/runs/{runId:guid}/dashboard"
)

foreach ($route in $expectedRoutes) {
    $match = $endpointMap | Where-Object { $_.routePattern -eq $route }

    if (-not $match) {
        throw "Missing route: $route"
    }

    Write-Host "Found route: $route"
}

Write-Host ""
Write-Host "Operational dashboard routes are mapped."
