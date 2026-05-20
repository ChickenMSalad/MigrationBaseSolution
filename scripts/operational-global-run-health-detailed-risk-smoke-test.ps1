param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$RecentLimit = 10,
    [int]$MetricsSampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "Requesting global operational run health detailed risk..."
Write-Host "GET $BaseUrl/api/operational/runs/health-detailed-risk?recentLimit=$RecentLimit&metricsSampleLimit=$MetricsSampleLimit"

$response = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/health-detailed-risk?recentLimit=$RecentLimit&metricsSampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

Write-Host "GeneratedAt: $($response.generatedAt)"
Write-Host "RiskScore: $($response.riskScore)"
Write-Host "RiskLevel: $($response.riskLevel)"
Write-Host "RiskPosture: $($response.riskPosture)"
Write-Host "BucketCount: $(@($response.buckets).Count)"
Write-Host "RecommendationCount: $(@($response.recommendations).Count)"

if ($response.riskScore -lt 0 -or $response.riskScore -gt 100) {
    throw "RiskScore must be between 0 and 100."
}

if (-not $response.riskLevel) {
    throw "RiskLevel is required."
}

if (-not $response.riskPosture) {
    throw "RiskPosture is required."
}

if (@($response.buckets).Count -lt 1) {
    throw "At least one risk bucket is required."
}

if (@($response.recommendations).Count -lt 1) {
    throw "At least one recommendation is required."
}

$response | ConvertTo-Json -Depth 35
