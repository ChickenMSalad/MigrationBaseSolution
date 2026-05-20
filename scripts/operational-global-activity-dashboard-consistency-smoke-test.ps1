param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$RecentLimit = 10,
    [int]$MetricsSampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "Loading component endpoints..."

$recent = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/activity/recent?limit=$RecentLimit" `
    -ContentType "application/json"

$metrics = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/activity/metrics?sampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

$catalog = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/timeline/catalog" `
    -ContentType "application/json"

Write-Host "Loading dashboard aggregate..."

$dashboard = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/activity/dashboard?recentLimit=$RecentLimit&metricsSampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

if ($dashboard.recentActivity.eventCount -ne $recent.eventCount) {
    throw "Dashboard recent activity count does not match recent endpoint."
}

if ($dashboard.metrics.totalEventCount -ne $metrics.totalEventCount) {
    throw "Dashboard metrics totalEventCount does not match metrics endpoint."
}

if ($dashboard.catalog.eventTypeCount -ne $catalog.eventTypeCount) {
    throw "Dashboard catalog eventTypeCount does not match catalog endpoint."
}

if ($dashboard.catalog.sourceCount -ne $catalog.sourceCount) {
    throw "Dashboard catalog sourceCount does not match catalog endpoint."
}

if ($dashboard.recentActivity.eventCount -gt $RecentLimit) {
    throw "Dashboard recent activity limit was not respected."
}

Write-Host "RecentActivityEventCount: $($dashboard.recentActivity.eventCount)"
Write-Host "MetricsTotalEventCount: $($dashboard.metrics.totalEventCount)"
Write-Host "CatalogEventTypeCount: $($dashboard.catalog.eventTypeCount)"
Write-Host "CatalogSourceCount: $($dashboard.catalog.sourceCount)"

Write-Host ""
Write-Host "Global operational activity dashboard consistency smoke passed."
