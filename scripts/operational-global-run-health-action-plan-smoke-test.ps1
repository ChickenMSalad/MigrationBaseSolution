param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$RecentLimit = 10,
    [int]$MetricsSampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "Requesting global operational run health action plan..."
Write-Host "GET $BaseUrl/api/operational/runs/health-action-plan?recentLimit=$RecentLimit&metricsSampleLimit=$MetricsSampleLimit"

$response = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/health-action-plan?recentLimit=$RecentLimit&metricsSampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

Write-Host "GeneratedAt: $($response.generatedAt)"
Write-Host "OverallPriority: $($response.overallPriority)"
Write-Host "ActionCount: $($response.actionCount)"
Write-Host "RecommendationCount: $($response.recommendations.recommendationCount)"

if ($response.actionCount -ne @($response.actions).Count) {
    throw "ActionCount does not match actions array length."
}

if ($response.actionCount -lt 1) {
    throw "At least one action is required."
}

foreach ($action in @($response.actions)) {
    if ($action.sequence -lt 1) {
        throw "Action sequence must be positive."
    }

    if (-not $action.actionKey) {
        throw "Action is missing actionKey."
    }

    if (-not $action.priority) {
        throw "Action is missing priority."
    }

    if (-not $action.title) {
        throw "Action is missing title."
    }

    if (-not $action.action) {
        throw "Action is missing action text."
    }

    if (-not $action.validationHint) {
        throw "Action is missing validationHint."
    }
}

$response | ConvertTo-Json -Depth 35
