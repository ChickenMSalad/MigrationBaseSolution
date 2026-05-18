param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

$url = "$BaseUrl/api/system/endpoints"

Write-Host "Requesting Admin API endpoint map..."
Write-Host "GET $url"

$endpoints = Invoke-RestMethod `
    -Method Get `
    -Uri $url `
    -ContentType "application/json"

Write-Host "Mapped endpoint count: $($endpoints.Count)"

$operationalEndpoints = @($endpoints | Where-Object {
    $_.routePattern -like "*operational*"
})

Write-Host "Operational endpoint count: $($operationalEndpoints.Count)"

$operationalEndpoints | ConvertTo-Json -Depth 10
