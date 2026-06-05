param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$RecentLimit = 10,
    [int]$MetricsSampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "Loading failure component endpoints..."

$recentFailures = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/recent?limit=$RecentLimit" `
    -ContentType "application/json"

$metrics = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/metrics?sampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

Write-Host "Loading failure dashboard aggregate..."

$dashboard = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/dashboard?recentLimit=$RecentLimit&metricsSampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

if ($dashboard.recentFailures.count -ne $recentFailures.count) {
    throw "Dashboard recent failure count does not match recent failures endpoint."
}

if ($dashboard.recentFailures.limit -ne $recentFailures.limit) {
    throw "Dashboard recent failure limit does not match recent failures endpoint."
}

if ($dashboard.metrics.totalFailureCount -ne $metrics.totalFailureCount) {
    throw "Dashboard metrics totalFailureCount does not match metrics endpoint."
}

if ($dashboard.metrics.retriableFailureCount -ne $metrics.retriableFailureCount) {
    throw "Dashboard metrics retriableFailureCount does not match metrics endpoint."
}

if ($dashboard.metrics.nonRetriableFailureCount -ne $metrics.nonRetriableFailureCount) {
    throw "Dashboard metrics nonRetriableFailureCount does not match metrics endpoint."
}

if ($dashboard.recentFailures.count -gt $RecentLimit) {
    throw "Dashboard recent failures limit was not respected."
}

Write-Host "RecentFailureCount: $($dashboard.recentFailures.count)"
Write-Host "MetricsTotalFailureCount: $($dashboard.metrics.totalFailureCount)"
Write-Host "RetriableFailureCount: $($dashboard.metrics.retriableFailureCount)"
Write-Host "NonRetriableFailureCount: $($dashboard.metrics.nonRetriableFailureCount)"

Write-Host ""
Write-Host "Global operational failure dashboard consistency smoke passed."
