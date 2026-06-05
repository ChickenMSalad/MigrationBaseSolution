param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$Limit = 10
)

$ErrorActionPreference = "Stop"

Write-Host "Requesting global operational failure filtered analytics..."
Write-Host "GET $BaseUrl/api/operational/failures/filtered-analytics?limit=$Limit"

$response = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/filtered-analytics?limit=$Limit" `
    -ContentType "application/json"

Write-Host "ResultCount: $($response.results.count)"
Write-Host "MetricsTotalFailureCount: $($response.metrics.totalFailureCount)"
Write-Host "RetriableFailureCount: $($response.metrics.retriableFailureCount)"
Write-Host "NonRetriableFailureCount: $($response.metrics.nonRetriableFailureCount)"
Write-Host "GeneratedAt: $($response.generatedAt)"

if ($response.results.count -gt $Limit) {
    throw "Filtered analytics result limit was not respected."
}

Write-Host ""
Write-Host "Requesting retriable filtered analytics..."
$retriable = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/filtered-analytics?isRetriable=true&limit=$Limit" `
    -ContentType "application/json"

foreach ($failure in @($retriable.results.failures)) {
    if (-not $failure.isRetriable) {
        throw "Retriable filtered analytics returned a non-retriable failure."
    }
}

$response | ConvertTo-Json -Depth 25
