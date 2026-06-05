param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

Write-Host "Checking Admin API endpoint map for dispatcher execution history routes..."

$endpointMap = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/system/endpoints" `
    -ContentType "application/json"

$expectedRoutes = @(
    "/api/operational/dispatcher/executions",
    "/api/operational/dispatcher/executions/{executionId:guid}",
    "/api/operational/dispatcher/executions/readiness"
)

foreach ($route in $expectedRoutes) {
    $match = $endpointMap | Where-Object { $_.routePattern -eq $route }

    if (-not $match) {
        throw "Missing route: $route"
    }

    Write-Host "Found route: $route"
}

Write-Host ""
Write-Host "Dispatcher execution history routes are mapped."
