param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$RecentLimit = 10,
    [int]$MetricsSampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "Requesting global operational run health recommendations..."
Write-Host "GET $BaseUrl/api/operational/runs/health-recommendations?recentLimit=$RecentLimit&metricsSampleLimit=$MetricsSampleLimit"

$response = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/health-recommendations?recentLimit=$RecentLimit&metricsSampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

Write-Host "GeneratedAt: $($response.generatedAt)"
Write-Host "RiskScore: $($response.detailedRisk.riskScore)"
Write-Host "RiskLevel: $($response.detailedRisk.riskLevel)"
Write-Host "RecommendationCount: $($response.recommendationCount)"

if ($response.recommendationCount -ne @($response.recommendations).Count) {
    throw "RecommendationCount does not match recommendations array length."
}

if ($response.recommendationCount -lt 1) {
    throw "At least one recommendation is required."
}

foreach ($recommendation in @($response.recommendations)) {
    if (-not $recommendation.recommendationKey) {
        throw "Recommendation is missing recommendationKey."
    }

    if (-not $recommendation.priority) {
        throw "Recommendation is missing priority."
    }

    if (-not $recommendation.title) {
        throw "Recommendation is missing title."
    }

    if (-not $recommendation.suggestedAction) {
        throw "Recommendation is missing suggestedAction."
    }
}

$response | ConvertTo-Json -Depth 35
