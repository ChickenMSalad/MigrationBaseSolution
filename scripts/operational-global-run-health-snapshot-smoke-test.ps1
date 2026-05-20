param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$RecentLimit = 10,
    [int]$MetricsSampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "Requesting global operational run health snapshot..."
Write-Host "GET $BaseUrl/api/operational/runs/health-snapshot?recentLimit=$RecentLimit&metricsSampleLimit=$MetricsSampleLimit"

$response = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/health-snapshot?recentLimit=$RecentLimit&metricsSampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

Write-Host "SnapshotAt: $($response.snapshotAt)"
Write-Host "TotalRunCount: $($response.summary.totalRunCount)"
Write-Host "RecentActivityEventCount: $($response.recentActivityEventCount)"
Write-Host "RecentFailureCount: $($response.recentFailureCount)"
Write-Host "FailureTypeCount: $($response.failureTypeCount)"
Write-Host "ActiveRiskScore: $($response.activeRiskScore)"
Write-Host "RiskLevel: $($response.riskLevel)"

if ($response.activeRiskScore -lt 0 -or $response.activeRiskScore -gt 100) {
    throw "ActiveRiskScore must be between 0 and 100."
}

if (-not $response.riskLevel) {
    throw "RiskLevel is required."
}

if (@($response.signals).Count -lt 1) {
    throw "Snapshot should include signals."
}

$response | ConvertTo-Json -Depth 25
