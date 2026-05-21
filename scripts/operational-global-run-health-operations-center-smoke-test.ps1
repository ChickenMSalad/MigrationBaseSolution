param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$ActivityRecentLimit = 10,
    [int]$MetricsSampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "Requesting global operational run health operations center..."
Write-Host "GET $BaseUrl/api/operational/runs/health-operations-center?activityRecentLimit=$ActivityRecentLimit&metricsSampleLimit=$MetricsSampleLimit"

$response = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/health-operations-center?activityRecentLimit=$ActivityRecentLimit&metricsSampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

Write-Host "GeneratedAt: $($response.generatedAt)"
Write-Host "RiskScore: $($response.detailedRisk.riskScore)"
Write-Host "RiskLevel: $($response.detailedRisk.riskLevel)"
Write-Host "OverallPriority: $($response.overallPriority)"
Write-Host "RecommendationCount: $($response.recommendations.recommendationCount)"
Write-Host "ActionCount: $($response.actionPlan.actionCount)"
Write-Host "SummaryMessageCount: $(@($response.summaryMessages).Count)"

if (-not $response.dashboard) {
    throw "Operations center dashboard is required."
}

if (-not $response.detailedRisk) {
    throw "Operations center detailedRisk is required."
}

if (-not $response.recommendations) {
    throw "Operations center recommendations are required."
}

if (-not $response.actionPlan) {
    throw "Operations center actionPlan is required."
}

if (@($response.summaryMessages).Count -lt 1) {
    throw "Operations center should include summary messages."
}

$response | ConvertTo-Json -Depth 40
