param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

Write-Host "Checking global operational failure analytics preset favorite routes..."

$endpointMap = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/system/endpoints" `
    -ContentType "application/json"

$routes = @(
    "/api/operational/failures/analytics-preset-favorites",
    "/api/operational/failures/analytics-preset-favorites/{favoriteKey}"
)

foreach ($route in $routes) {
    $match = $endpointMap | Where-Object { $_.routePattern -eq $route }

    if (-not $match) {
        throw "Missing route: $route"
    }

    Write-Host "Found route: $route"
}
