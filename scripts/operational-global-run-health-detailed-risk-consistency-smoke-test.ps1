param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$RecentLimit = 10,
    [int]$MetricsSampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "Loading trend summary..."
$trend = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/health-trend-summary?recentLimit=$RecentLimit&metricsSampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

Write-Host "Loading detailed risk..."
$risk = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/health-detailed-risk?recentLimit=$RecentLimit&metricsSampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

if ($risk.trendSummary.currentSnapshot.summary.totalRunCount -ne $trend.currentSnapshot.summary.totalRunCount) {
    throw "Detailed risk trend summary totalRunCount does not match trend summary endpoint."
}

if ($risk.trendSummary.recentFailureCount -ne $trend.recentFailureCount) {
    throw "Detailed risk recentFailureCount does not match trend summary endpoint."
}

if ($risk.riskScore -lt $trend.riskScore) {
    throw "Detailed risk score should not be lower than trend risk score."
}

$expectedRiskLevel = if ($risk.riskScore -ge 75) {
    "Critical"
}
elseif ($risk.riskScore -ge 50) {
    "High"
}
elseif ($risk.riskScore -ge 25) {
    "Elevated"
}
else {
    "Normal"
}

if ($risk.riskLevel -ne $expectedRiskLevel) {
    throw "Detailed risk level does not match risk score thresholds."
}

foreach ($bucket in @($risk.buckets)) {
    if (-not $bucket.bucketKey) {
        throw "Risk bucket is missing bucketKey."
    }

    if (-not $bucket.displayName) {
        throw "Risk bucket is missing displayName."
    }

    if (-not $bucket.severity) {
        throw "Risk bucket is missing severity."
    }

    if ($bucket.scoreContribution -lt 0 -or $bucket.scoreContribution -gt 100) {
        throw "Risk bucket scoreContribution must be between 0 and 100."
    }

    if ($bucket.count -lt 0) {
        throw "Risk bucket count cannot be negative."
    }
}

Write-Host "TrendRiskScore: $($trend.riskScore)"
Write-Host "DetailedRiskScore: $($risk.riskScore)"
Write-Host "RiskLevel: $($risk.riskLevel)"
Write-Host "BucketCount: $(@($risk.buckets).Count)"
Write-Host "RecommendationCount: $(@($risk.recommendations).Count)"
Write-Host ""
Write-Host "Global operational run health detailed risk consistency smoke passed."
