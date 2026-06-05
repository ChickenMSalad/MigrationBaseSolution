param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$RecentLimit = 10,
    [int]$MetricsSampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "Loading detailed risk..."
$risk = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/health-detailed-risk?recentLimit=$RecentLimit&metricsSampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

Write-Host "Loading recommendations..."
$recommendations = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/health-recommendations?recentLimit=$RecentLimit&metricsSampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

if ($recommendations.detailedRisk.riskScore -ne $risk.riskScore) {
    throw "Recommendation detailed risk score does not match detailed risk endpoint."
}

if ($recommendations.detailedRisk.riskLevel -ne $risk.riskLevel) {
    throw "Recommendation detailed risk level does not match detailed risk endpoint."
}

if ($recommendations.recommendationCount -ne @($recommendations.recommendations).Count) {
    throw "RecommendationCount does not match recommendations array length."
}

$validPriorities = @("High", "Medium", "Low", "Informational")
foreach ($recommendation in @($recommendations.recommendations)) {
    if (-not ($validPriorities -contains $recommendation.priority)) {
        throw "Invalid recommendation priority: $($recommendation.priority)"
    }
}

if ($risk.riskScore -eq 0) {
    $continueMonitoring = @($recommendations.recommendations) |
        Where-Object { $_.recommendationKey -eq "continue-monitoring" }

    if (-not $continueMonitoring) {
        throw "Healthy risk state should include continue-monitoring recommendation."
    }
}

Write-Host "DetailedRiskScore: $($risk.riskScore)"
Write-Host "RecommendationCount: $($recommendations.recommendationCount)"
Write-Host ""
Write-Host "Global operational run health recommendations consistency smoke passed."
