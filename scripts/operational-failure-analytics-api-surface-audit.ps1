param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Operational Failure Analytics API Surface Audit ==="
Write-Host "GET $BaseUrl/api/system/endpoints"

$endpointMap = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/system/endpoints" `
    -ContentType "application/json"

$expectedRoutes = @(
    "/api/operational/failures/recent",
    "/api/operational/failures/metrics",
    "/api/operational/failures/dashboard",
    "/api/operational/failures/query",
    "/api/operational/failures/catalog",
    "/api/operational/failures/system-pair-metrics",
    "/api/operational/failures/run-status-metrics",
    "/api/operational/failures/analytics-dashboard",
    "/api/operational/failures/filtered-analytics",
    "/api/operational/failures/analytics-presets",
    "/api/operational/failures/analytics-presets/{presetKey}",
    "/api/operational/failures/analytics-presets/search",
    "/api/operational/failures/analytics-preset-dashboard",
    "/api/operational/failures/analytics-preset-favorites",
    "/api/operational/failures/analytics-preset-favorites/{favoriteKey}"
)

$missing = New-Object System.Collections.Generic.List[string]
$found = New-Object System.Collections.Generic.List[string]

foreach ($route in $expectedRoutes) {
    $match = $endpointMap | Where-Object { $_.routePattern -eq $route }

    if ($match) {
        $found.Add($route)
    }
    else {
        $missing.Add($route)
    }
}

Write-Host ""
Write-Host "Expected failure analytics route count: $($expectedRoutes.Count)"
Write-Host "Found: $($found.Count)"
Write-Host "Missing: $($missing.Count)"

if ($found.Count -gt 0) {
    Write-Host ""
    Write-Host "Found routes:"
    foreach ($route in $found) {
        Write-Host " + $route"
    }
}

if ($missing.Count -gt 0) {
    Write-Host ""
    Write-Host "Missing routes:"
    foreach ($route in $missing) {
        Write-Host " - $route"
    }

    throw "Operational failure analytics API surface audit failed. Missing route count: $($missing.Count)"
}

Write-Host ""
Write-Host "Checking for accidental double /api prefixes under operational failure routes..."

$doubleApiRoutes = $endpointMap |
    Where-Object { $_.routePattern -like "/api/api/operational/failures/*" } |
    Sort-Object routePattern

if ($doubleApiRoutes) {
    Write-Host ""
    Write-Host "Unexpected double-prefix routes:"
    foreach ($route in $doubleApiRoutes) {
        Write-Host " - $($route.routePattern)"
    }

    throw "Operational failure analytics API surface audit failed. Double /api route prefixes found."
}

Write-Host "No double-prefix failure routes found."

Write-Host ""
Write-Host "Operational failure analytics API surface audit passed."
