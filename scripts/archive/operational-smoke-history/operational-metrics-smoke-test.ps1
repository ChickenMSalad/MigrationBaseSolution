param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

$endpoints = @(
    "/api/operational/metrics/work-items",
    "/api/operational/metrics/leases",
    "/api/operational/metrics/runs",
    "/api/operational/diagnostics/summary"
)

foreach ($endpoint in $endpoints) {
    $url = "$BaseUrl$endpoint"

    Write-Host ""
    Write-Host "GET $url"

    $response = Invoke-RestMethod `
        -Method Get `
        -Uri $url `
        -ContentType "application/json"

    $response | ConvertTo-Json -Depth 10
}
