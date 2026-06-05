param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$RecentLimit = 10,
    [int]$MetricsSampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "Requesting global operational failure analytics dashboard..."
Write-Host "GET $BaseUrl/api/operational/failures/analytics-dashboard?recentLimit=$RecentLimit&metricsSampleLimit=$MetricsSampleLimit"

$response = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/analytics-dashboard?recentLimit=$RecentLimit&metricsSampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

Write-Host "RecentFailureCount: $($response.dashboard.recentFailures.count)"
Write-Host "MetricsTotalFailureCount: $($response.dashboard.metrics.totalFailureCount)"
Write-Host "SystemPairCount: $($response.systemPairMetrics.systemPairCount)"
Write-Host "RunStatusCount: $($response.runStatusMetrics.runStatusCount)"
Write-Host "FailureTypeCount: $($response.catalog.failureTypeCount)"
Write-Host "GeneratedAt: $($response.generatedAt)"

if ($response.dashboard.recentFailures.count -gt $RecentLimit) {
    throw "Failure analytics dashboard recent failures limit was not respected."
}

if ($response.messages) {
    Write-Host ""
    Write-Host "Messages:"
    foreach ($message in $response.messages) {
        Write-Host "- $message"
    }
}

$response | ConvertTo-Json -Depth 30
