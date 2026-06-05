param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$RecentLimit = 10,
    [int]$MetricsSampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "Loading run health snapshot..."
$snapshot = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/health-snapshot?recentLimit=$RecentLimit&metricsSampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

Write-Host "Loading run health trend summary..."
$trend = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/health-trend-summary?recentLimit=$RecentLimit&metricsSampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

if (-not $trend.currentSnapshot) {
    throw "Trend summary is missing currentSnapshot."
}

if ($trend.currentSnapshot.summary.totalRunCount -ne $snapshot.summary.totalRunCount) {
    throw "Trend currentSnapshot totalRunCount does not match snapshot endpoint."
}

if ($trend.currentSnapshot.summary.totalWorkItemCount -ne $snapshot.summary.totalWorkItemCount) {
    throw "Trend currentSnapshot totalWorkItemCount does not match snapshot endpoint."
}

if ($trend.currentSnapshot.summary.totalFailureCount -ne $snapshot.summary.totalFailureCount) {
    throw "Trend currentSnapshot totalFailureCount does not match snapshot endpoint."
}

if ($trend.recentActivityEventCount -ne $snapshot.recentActivityEventCount) {
    throw "Trend recentActivityEventCount does not match snapshot endpoint."
}

if ($trend.recentFailureCount -ne $snapshot.recentFailureCount) {
    throw "Trend recentFailureCount does not match snapshot endpoint."
}

if ($trend.activeRunCount -ne $snapshot.summary.activeRunCount) {
    throw "Trend activeRunCount does not match snapshot summary."
}

if ($trend.outstandingWorkItemCount -ne $snapshot.summary.outstandingWorkItemCount) {
    throw "Trend outstandingWorkItemCount does not match snapshot summary."
}

if ($trend.lockedWorkItemCount -ne $snapshot.summary.lockedWorkItemCount) {
    throw "Trend lockedWorkItemCount does not match snapshot summary."
}

if ($trend.riskScore -lt 0 -or $trend.riskScore -gt 100) {
    throw "Trend riskScore must be between 0 and 100."
}

$expectedRiskLevel = if ($trend.riskScore -ge 75) {
    "Critical"
}
elseif ($trend.riskScore -ge 50) {
    "High"
}
elseif ($trend.riskScore -ge 25) {
    "Elevated"
}
else {
    "Normal"
}

if ($trend.riskLevel -ne $expectedRiskLevel) {
    throw "Trend riskLevel does not match riskScore thresholds."
}

$validDirections = @("Worsening", "Watch", "Stable", "Neutral")
if (-not ($validDirections -contains $trend.trendDirection)) {
    throw "TrendDirection is invalid: $($trend.trendDirection)"
}

if (-not $trend.trendMessage) {
    throw "TrendMessage is required."
}

foreach ($signal in @($trend.signals)) {
    if (-not $signal.signalKey) {
        throw "Trend signal is missing signalKey."
    }

    if (-not $signal.severity) {
        throw "Trend signal is missing severity."
    }

    if (-not $signal.message) {
        throw "Trend signal is missing message."
    }

    if ($signal.weight -lt 0 -or $signal.weight -gt 100) {
        throw "Trend signal weight must be between 0 and 100."
    }
}

Write-Host "TrendDirection: $($trend.trendDirection)"
Write-Host "TrendMessage: $($trend.trendMessage)"
Write-Host "RiskScore: $($trend.riskScore)"
Write-Host "RiskLevel: $($trend.riskLevel)"
Write-Host "RecentActivityEventCount: $($trend.recentActivityEventCount)"
Write-Host "RecentFailureCount: $($trend.recentFailureCount)"
Write-Host "SignalCount: $(@($trend.signals).Count)"

Write-Host ""
Write-Host "Global operational run health trend summary consistency smoke passed."
