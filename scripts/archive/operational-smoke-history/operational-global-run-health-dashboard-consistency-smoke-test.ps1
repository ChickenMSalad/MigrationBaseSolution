param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$ActivityRecentLimit = 10,
    [int]$MetricsSampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "Loading component endpoints..."

$summary = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/health-summary" `
    -ContentType "application/json"

$activity = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/activity/dashboard?recentLimit=$ActivityRecentLimit&metricsSampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

$failureAnalytics = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/analytics-dashboard?recentLimit=$ActivityRecentLimit&metricsSampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

Write-Host "Loading run health dashboard aggregate..."

$dashboard = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/health-dashboard?activityRecentLimit=$ActivityRecentLimit&metricsSampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

# Support either the original Set 128 names or locally adjusted names.
$dashboardSummary = if ($dashboard.summary) { $dashboard.summary } else { $dashboard.healthSummary }
$dashboardActivity = if ($dashboard.activity) { $dashboard.activity } else { $dashboard.activityDashboard }
$dashboardFailureAnalytics = $dashboard.failureAnalytics

if (-not $dashboardSummary) {
    throw "Run health dashboard does not contain summary/healthSummary."
}

if (-not $dashboardActivity) {
    throw "Run health dashboard does not contain activity/activityDashboard."
}

if (-not $dashboardFailureAnalytics) {
    throw "Run health dashboard does not contain failureAnalytics."
}

if ($dashboardSummary.totalRunCount -ne $summary.totalRunCount) {
    throw "Dashboard totalRunCount does not match health summary endpoint."
}

if ($dashboardSummary.totalWorkItemCount -ne $summary.totalWorkItemCount) {
    throw "Dashboard totalWorkItemCount does not match health summary endpoint."
}

if ($dashboardSummary.totalFailureCount -ne $summary.totalFailureCount) {
    throw "Dashboard totalFailureCount does not match health summary endpoint."
}

if ($dashboardSummary.completionPercent -ne $summary.completionPercent) {
    throw "Dashboard completionPercent does not match health summary endpoint."
}

if ($dashboardActivity.recentActivity.eventCount -ne $activity.recentActivity.eventCount) {
    throw "Dashboard recent activity count does not match activity dashboard endpoint."
}

if ($dashboardActivity.metrics.totalEventCount -ne $activity.metrics.totalEventCount) {
    throw "Dashboard activity metrics totalEventCount does not match activity dashboard endpoint."
}

if ($dashboardActivity.recentActivity.eventCount -gt $ActivityRecentLimit) {
    throw "Dashboard activity recent limit was not respected."
}

if ($dashboardFailureAnalytics.dashboard.recentFailures.count -ne $failureAnalytics.dashboard.recentFailures.count) {
    throw "Dashboard recent failure count does not match failure analytics dashboard endpoint."
}

if ($dashboardFailureAnalytics.dashboard.metrics.totalFailureCount -ne $failureAnalytics.dashboard.metrics.totalFailureCount) {
    throw "Dashboard failure metrics totalFailureCount does not match failure analytics dashboard endpoint."
}

if ($dashboardFailureAnalytics.systemPairMetrics.systemPairCount -ne $failureAnalytics.systemPairMetrics.systemPairCount) {
    throw "Dashboard failure systemPairCount does not match failure analytics dashboard endpoint."
}

if ($dashboardFailureAnalytics.runStatusMetrics.runStatusCount -ne $failureAnalytics.runStatusMetrics.runStatusCount) {
    throw "Dashboard failure runStatusCount does not match failure analytics dashboard endpoint."
}

if ($dashboardSummary.completionPercent -lt 0 -or $dashboardSummary.completionPercent -gt 100) {
    throw "Dashboard completionPercent must be between 0 and 100."
}

if (@($dashboard.messages).Count -lt 1) {
    throw "Dashboard should include generated messages."
}

Write-Host "TotalRunCount: $($dashboardSummary.totalRunCount)"
Write-Host "TotalWorkItemCount: $($dashboardSummary.totalWorkItemCount)"
Write-Host "TotalFailureCount: $($dashboardSummary.totalFailureCount)"
Write-Host "CompletionPercent: $($dashboardSummary.completionPercent)"
Write-Host "RecentActivityEventCount: $($dashboardActivity.recentActivity.eventCount)"
Write-Host "FailureAnalyticsTotalFailureCount: $($dashboardFailureAnalytics.dashboard.metrics.totalFailureCount)"
Write-Host "MessageCount: $(@($dashboard.messages).Count)"

Write-Host ""
Write-Host "Global operational run health dashboard consistency smoke passed."
