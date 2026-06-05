param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

Write-Host "Checking restored operational diagnostics routes..."

$endpointMap = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/system/endpoints" `
    -ContentType "application/json"

$expectedRoutes = @(
    "/api/operational/mirror/enablement-guard",
    "/api/operational/mirror/last-invocation",
    "/api/operational/mirror/readiness",
    "/api/operational/mirror/status",
    "/api/operational/mirror/write-verification",
    "/api/operational/sql/schema/smoke-test"
)

foreach ($route in $expectedRoutes) {
    $match = $endpointMap | Where-Object { $_.routePattern -eq $route }

    if (-not $match) {
        throw "Missing restored diagnostic route: $route"
    }

    Write-Host "Found route: $route"
}

Write-Host ""
Write-Host "Operational diagnostics routes are restored."
