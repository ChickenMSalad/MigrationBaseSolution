param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$RecentLimit = 10,
    [int]$MetricsSampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "Loading recommendations..."
$recommendations = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/health-recommendations?recentLimit=$RecentLimit&metricsSampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

Write-Host "Loading action plan..."
$actionPlan = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/health-action-plan?recentLimit=$RecentLimit&metricsSampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

if ($actionPlan.recommendations.recommendationCount -ne $recommendations.recommendationCount) {
    throw "Action plan recommendation count does not match recommendations endpoint."
}

if ($actionPlan.actionCount -ne $recommendations.recommendationCount) {
    throw "Action count should match recommendation count."
}

$recommendationKeys = @{}
foreach ($recommendation in @($recommendations.recommendations)) {
    $recommendationKeys[$recommendation.recommendationKey] = $true
}

foreach ($action in @($actionPlan.actions)) {
    if (-not $recommendationKeys.ContainsKey($action.sourceRecommendationKey)) {
        throw "Action references unknown recommendation key: $($action.sourceRecommendationKey)"
    }
}

$validPriorities = @("High", "Medium", "Low", "Informational")
if (-not ($validPriorities -contains $actionPlan.overallPriority)) {
    throw "Invalid overall priority: $($actionPlan.overallPriority)"
}

Write-Host "RecommendationCount: $($recommendations.recommendationCount)"
Write-Host "ActionCount: $($actionPlan.actionCount)"
Write-Host "OverallPriority: $($actionPlan.overallPriority)"
Write-Host ""
Write-Host "Global operational run health action plan consistency smoke passed."
