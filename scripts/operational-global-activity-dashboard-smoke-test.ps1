param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$RecentLimit = 10,
    [int]$MetricsSampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "Requesting global operational activity dashboard..."
Write-Host "GET $BaseUrl/api/operational/activity/dashboard?recentLimit=$RecentLimit&metricsSampleLimit=$MetricsSampleLimit"

$response = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/activity/dashboard?recentLimit=$RecentLimit&metricsSampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

Write-Host "RecentActivityEventCount: $($response.recentActivity.eventCount)"
Write-Host "MetricsTotalEventCount: $($response.metrics.totalEventCount)"
Write-Host "CatalogEventTypeCount: $($response.catalog.eventTypeCount)"
Write-Host "CatalogSourceCount: $($response.catalog.sourceCount)"
Write-Host "GeneratedAt: $($response.generatedAt)"

if ($response.recentActivity.eventCount -gt $RecentLimit) {
    throw "Dashboard recent activity limit was not respected."
}

if ($response.messages) {
    Write-Host ""
    Write-Host "Messages:"
    foreach ($message in $response.messages) {
        Write-Host "- $message"
    }
}

$response | ConvertTo-Json -Depth 25
