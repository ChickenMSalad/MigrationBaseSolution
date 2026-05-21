param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$ActivityRecentLimit = 10,
    [int]$MetricsSampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "Loading component endpoints..."

$dashboard = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/health-dashboard?activityRecentLimit=$ActivityRecentLimit&metricsSampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

$detailedRisk = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/health-detailed-risk?recentLimit=$ActivityRecentLimit&metricsSampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

$recommendations = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/health-recommendations?recentLimit=$ActivityRecentLimit&metricsSampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

$actionPlan = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/health-action-plan?recentLimit=$ActivityRecentLimit&metricsSampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

Write-Host "Loading operations center aggregate..."

$center = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/health-operations-center?activityRecentLimit=$ActivityRecentLimit&metricsSampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

if ($center.dashboard.healthSummary.totalRunCount -ne $dashboard.healthSummary.totalRunCount) {
    throw "Operations center dashboard totalRunCount does not match dashboard endpoint."
}

if ($center.detailedRisk.riskScore -ne $detailedRisk.riskScore) {
    throw "Operations center riskScore does not match detailed risk endpoint."
}

if ($center.detailedRisk.riskLevel -ne $detailedRisk.riskLevel) {
    throw "Operations center riskLevel does not match detailed risk endpoint."
}

if ($center.recommendations.recommendationCount -ne $recommendations.recommendationCount) {
    throw "Operations center recommendationCount does not match recommendations endpoint."
}

if ($center.actionPlan.actionCount -ne $actionPlan.actionCount) {
    throw "Operations center actionCount does not match action plan endpoint."
}

if ($center.overallPriority -ne $actionPlan.overallPriority) {
    throw "Operations center overallPriority does not match action plan endpoint."
}

Write-Host "TotalRunCount: $($center.dashboard.healthSummary.totalRunCount)"
Write-Host "RiskScore: $($center.detailedRisk.riskScore)"
Write-Host "RiskLevel: $($center.detailedRisk.riskLevel)"
Write-Host "RecommendationCount: $($center.recommendations.recommendationCount)"
Write-Host "ActionCount: $($center.actionPlan.actionCount)"
Write-Host "OverallPriority: $($center.overallPriority)"
Write-Host ""
Write-Host "Global operational run health operations center consistency smoke passed."
