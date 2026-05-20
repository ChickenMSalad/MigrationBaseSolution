param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$RecentLimit = 10,
    [int]$MetricsSampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "Requesting global operational run health trend summary..."
Write-Host "GET $BaseUrl/api/operational/runs/health-trend-summary?recentLimit=$RecentLimit&metricsSampleLimit=$MetricsSampleLimit"

$response = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/health-trend-summary?recentLimit=$RecentLimit&metricsSampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

Write-Host "GeneratedAt: $($response.generatedAt)"
Write-Host "TrendDirection: $($response.trendDirection)"
Write-Host "TrendMessage: $($response.trendMessage)"
Write-Host "RiskScore: $($response.riskScore)"
Write-Host "RiskLevel: $($response.riskLevel)"
Write-Host "RecentActivityEventCount: $($response.recentActivityEventCount)"
Write-Host "RecentFailureCount: $($response.recentFailureCount)"
Write-Host "ActiveRunCount: $($response.activeRunCount)"
Write-Host "OutstandingWorkItemCount: $($response.outstandingWorkItemCount)"
Write-Host "LockedWorkItemCount: $($response.lockedWorkItemCount)"

if ($response.riskScore -lt 0 -or $response.riskScore -gt 100) {
    throw "RiskScore must be between 0 and 100."
}

if (-not $response.trendDirection) {
    throw "TrendDirection is required."
}

if (-not $response.trendMessage) {
    throw "TrendMessage is required."
}

if (-not $response.riskLevel) {
    throw "RiskLevel is required."
}

$response | ConvertTo-Json -Depth 30
