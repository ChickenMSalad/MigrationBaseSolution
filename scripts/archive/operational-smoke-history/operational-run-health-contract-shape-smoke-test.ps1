param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$Limit = 10,
    [int]$MetricsSampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "=== Operational Run Health Contract Shape Smoke ==="

$summary = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/health-summary" `
    -ContentType "application/json"

$dashboard = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/health-dashboard?activityRecentLimit=$Limit&metricsSampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

$snapshot = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/health-snapshot?recentLimit=$Limit&metricsSampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

$trend = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/health-trend-summary?recentLimit=$Limit&metricsSampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

$risk = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/health-detailed-risk?recentLimit=$Limit&metricsSampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

$recommendations = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/health-recommendations?recentLimit=$Limit&metricsSampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

$actionPlan = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/health-action-plan?recentLimit=$Limit&metricsSampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

$center = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/health-operations-center?activityRecentLimit=$Limit&metricsSampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

if ($null -eq $summary.totalRunCount) {
    throw "health-summary missing totalRunCount."
}

$dashboardSummary = if ($dashboard.healthSummary) { $dashboard.healthSummary } else { $dashboard.summary }
$dashboardActivity = if ($dashboard.activityDashboard) { $dashboard.activityDashboard } else { $dashboard.activity }

if (-not $dashboardSummary) {
    throw "health-dashboard missing healthSummary/summary."
}

if (-not $dashboardActivity) {
    throw "health-dashboard missing activityDashboard/activity."
}

if (-not $dashboard.failureAnalytics) {
    throw "health-dashboard missing failureAnalytics."
}

if (-not $snapshot.summary) {
    throw "health-snapshot missing summary."
}

if (-not $trend.currentSnapshot) {
    throw "health-trend-summary missing currentSnapshot."
}

if (-not $risk.trendSummary) {
    throw "health-detailed-risk missing trendSummary."
}

if (-not $recommendations.detailedRisk) {
    throw "health-recommendations missing detailedRisk."
}

if (-not $actionPlan.recommendations) {
    throw "health-action-plan missing recommendations."
}

if (-not $center.dashboard) {
    throw "health-operations-center missing dashboard."
}

if (-not $center.detailedRisk) {
    throw "health-operations-center missing detailedRisk."
}

if (-not $center.recommendations) {
    throw "health-operations-center missing recommendations."
}

if (-not $center.actionPlan) {
    throw "health-operations-center missing actionPlan."
}

Write-Host "HealthSummary.TotalRunCount: $($summary.totalRunCount)"
Write-Host "Dashboard.TotalRunCount: $($dashboardSummary.totalRunCount)"
Write-Host "Snapshot.RiskScore: $($snapshot.activeRiskScore)"
Write-Host "Trend.RiskScore: $($trend.riskScore)"
Write-Host "DetailedRisk.RiskScore: $($risk.riskScore)"
Write-Host "Recommendations.Count: $($recommendations.recommendationCount)"
Write-Host "ActionPlan.Count: $($actionPlan.actionCount)"
Write-Host "OperationsCenter.Priority: $($center.overallPriority)"

Write-Host ""
Write-Host "Operational run-health contract shape smoke passed."
