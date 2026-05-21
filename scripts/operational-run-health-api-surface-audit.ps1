param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Operational Run Health API Surface Audit ==="
Write-Host "GET $BaseUrl/api/system/endpoints"

$endpointMap = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/system/endpoints" `
    -ContentType "application/json"

$expectedRoutes = @(
    "/api/operational/runs/health-summary",
    "/api/operational/runs/health-dashboard",
    "/api/operational/runs/health-snapshot",
    "/api/operational/runs/health-trend-summary",
    "/api/operational/runs/health-detailed-risk",
    "/api/operational/runs/health-recommendations",
    "/api/operational/runs/health-action-plan",
    "/api/operational/runs/health-operations-center"
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
Write-Host "Expected run-health route count: $($expectedRoutes.Count)"
Write-Host "Found: $($found.Count)"
Write-Host "Missing: $($missing.Count)"

Write-Host ""
Write-Host "Found routes:"
foreach ($route in $found) {
    Write-Host " + $route"
}

if ($missing.Count -gt 0) {
    Write-Host ""
    Write-Host "Missing routes:"
    foreach ($route in $missing) {
        Write-Host " - $route"
    }

    throw "Operational run-health API surface audit failed. Missing route count: $($missing.Count)"
}

Write-Host ""
Write-Host "Checking for accidental double /api prefixes under run-health routes..."

$doubleApiRoutes = $endpointMap |
    Where-Object { $_.routePattern -like "/api/api/operational/runs/health*" } |
    Sort-Object routePattern

if ($doubleApiRoutes) {
    Write-Host ""
    Write-Host "Unexpected double-prefix routes:"
    foreach ($route in $doubleApiRoutes) {
        Write-Host " - $($route.routePattern)"
    }

    throw "Operational run-health API surface audit failed. Double /api route prefixes found."
}

Write-Host "No double-prefix run-health routes found."
Write-Host ""
Write-Host "Operational run-health API surface audit passed."
