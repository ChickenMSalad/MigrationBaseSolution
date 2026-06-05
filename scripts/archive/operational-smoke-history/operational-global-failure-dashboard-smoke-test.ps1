param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$RecentLimit = 10,
    [int]$MetricsSampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "Requesting global operational failure dashboard..."
Write-Host "GET $BaseUrl/api/operational/failures/dashboard?recentLimit=$RecentLimit&metricsSampleLimit=$MetricsSampleLimit"

$response = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/dashboard?recentLimit=$RecentLimit&metricsSampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

Write-Host "RecentFailureCount: $($response.recentFailures.count)"
Write-Host "MetricsTotalFailureCount: $($response.metrics.totalFailureCount)"
Write-Host "RetriableFailureCount: $($response.metrics.retriableFailureCount)"
Write-Host "NonRetriableFailureCount: $($response.metrics.nonRetriableFailureCount)"
Write-Host "GeneratedAt: $($response.generatedAt)"

if ($response.recentFailures.count -gt $RecentLimit) {
    throw "Failure dashboard recent failures limit was not respected."
}

if ($response.messages) {
    Write-Host ""
    Write-Host "Messages:"
    foreach ($message in $response.messages) {
        Write-Host "- $message"
    }
}

$response | ConvertTo-Json -Depth 25
