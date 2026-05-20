param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$ActivityLimit = 10,
    [int]$FailureLimit = 10,
    [int]$MetricsSampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "Requesting global operational run health dashboard..."
Write-Host "GET $BaseUrl/api/operational/runs/health-dashboard?activityLimit=$ActivityLimit&failureLimit=$FailureLimit&metricsSampleLimit=$MetricsSampleLimit"

$response = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/health-dashboard?activityLimit=$ActivityLimit&failureLimit=$FailureLimit&metricsSampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

Write-Host "TotalRunCount: $($response.healthSummary.totalRunCount)"
Write-Host "TotalWorkItemCount: $($response.healthSummary.totalWorkItemCount)"
Write-Host "TotalFailureCount: $($response.healthSummary.totalFailureCount)"
Write-Host "RecentActivityEventCount: $($response.activityDashboard.recentActivity.eventCount)"
Write-Host "FailureAnalyticsTotalFailureCount: $($response.failureAnalytics.dashboard.metrics.totalFailureCount)"
Write-Host "GeneratedAt: $($response.generatedAt)"

if ($response.activityDashboard.recentActivity.eventCount -gt $ActivityLimit) {
    throw "Run health dashboard activity limit was not respected."
}

if ($response.failureAnalytics.dashboard.recentFailures.count -gt $FailureLimit) {
    throw "Run health dashboard failure limit was not respected."
}

if ($response.healthSummary.completionPercent -lt 0 -or $response.healthSummary.completionPercent -gt 100) {
    throw "Run health dashboard completionPercent must be between 0 and 100."
}

$response | ConvertTo-Json -Depth 35
