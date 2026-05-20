param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$RecentLimit = 10,
    [int]$MetricsSampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "Loading component endpoints..."

$summary = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/health-summary" `
    -ContentType "application/json"

$activity = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/activity/recent?limit=$RecentLimit" `
    -ContentType "application/json"

$failureDashboard = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/dashboard?recentLimit=$RecentLimit&metricsSampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

Write-Host "Loading run health snapshot aggregate..."

$snapshot = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/health-snapshot?recentLimit=$RecentLimit&metricsSampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

if (-not $snapshot.snapshotAt) {
    throw "SnapshotAt is required."
}

if ($snapshot.summary.totalRunCount -ne $summary.totalRunCount) {
    throw "Snapshot summary totalRunCount does not match health summary endpoint."
}

if ($snapshot.summary.totalWorkItemCount -ne $summary.totalWorkItemCount) {
    throw "Snapshot summary totalWorkItemCount does not match health summary endpoint."
}

if ($snapshot.summary.totalFailureCount -ne $summary.totalFailureCount) {
    throw "Snapshot summary totalFailureCount does not match health summary endpoint."
}

if ($snapshot.summary.completionPercent -ne $summary.completionPercent) {
    throw "Snapshot summary completionPercent does not match health summary endpoint."
}

if ($snapshot.recentActivityEventCount -ne $activity.eventCount) {
    throw "Snapshot recentActivityEventCount does not match recent activity feed."
}

if ($snapshot.recentActivityEventCount -gt $RecentLimit) {
    throw "Snapshot recent activity limit was not respected."
}

if ($snapshot.recentFailureCount -ne $failureDashboard.recentFailures.count) {
    throw "Snapshot recentFailureCount does not match failure dashboard."
}

if ($snapshot.failureTypeCount -ne @($failureDashboard.metrics.failureTypes).Count) {
    throw "Snapshot failureTypeCount does not match failure dashboard metrics."
}

if ($snapshot.activeRiskScore -lt 0 -or $snapshot.activeRiskScore -gt 100) {
    throw "Snapshot activeRiskScore must be between 0 and 100."
}

$validRiskLevels = @("Normal", "Elevated", "High", "Critical")
if (-not ($validRiskLevels -contains $snapshot.riskLevel)) {
    throw "Snapshot riskLevel is not valid: $($snapshot.riskLevel)"
}

if (@($snapshot.signals).Count -lt 1) {
    throw "Snapshot should include at least one signal."
}

$expectedRiskLevel = if ($snapshot.activeRiskScore -ge 75) {
    "Critical"
}
elseif ($snapshot.activeRiskScore -ge 50) {
    "High"
}
elseif ($snapshot.activeRiskScore -ge 25) {
    "Elevated"
}
else {
    "Normal"
}

if ($snapshot.riskLevel -ne $expectedRiskLevel) {
    throw "Snapshot riskLevel does not match activeRiskScore thresholds."
}

Write-Host "SnapshotAt: $($snapshot.snapshotAt)"
Write-Host "TotalRunCount: $($snapshot.summary.totalRunCount)"
Write-Host "RecentActivityEventCount: $($snapshot.recentActivityEventCount)"
Write-Host "RecentFailureCount: $($snapshot.recentFailureCount)"
Write-Host "FailureTypeCount: $($snapshot.failureTypeCount)"
Write-Host "ActiveRiskScore: $($snapshot.activeRiskScore)"
Write-Host "RiskLevel: $($snapshot.riskLevel)"
Write-Host "SignalCount: $(@($snapshot.signals).Count)"

Write-Host ""
Write-Host "Global operational run health snapshot consistency smoke passed."
