param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$RecentLimit = 10,
    [int]$MetricsSampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "Loading failure analytics component endpoints..."

$dashboard = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/dashboard?recentLimit=$RecentLimit&metricsSampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

$systemPairMetrics = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/system-pair-metrics?sampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

$runStatusMetrics = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/run-status-metrics?sampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

$catalog = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/catalog?sampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

Write-Host "Loading failure analytics dashboard aggregate..."

$aggregate = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/analytics-dashboard?recentLimit=$RecentLimit&metricsSampleLimit=$MetricsSampleLimit" `
    -ContentType "application/json"

if ($aggregate.dashboard.recentFailures.count -ne $dashboard.recentFailures.count) {
    throw "Analytics dashboard recent failure count does not match failure dashboard endpoint."
}

if ($aggregate.dashboard.metrics.totalFailureCount -ne $dashboard.metrics.totalFailureCount) {
    throw "Analytics dashboard core failure metrics do not match failure dashboard endpoint."
}

if ($aggregate.systemPairMetrics.totalFailureCount -ne $systemPairMetrics.totalFailureCount) {
    throw "Analytics dashboard system-pair totalFailureCount does not match component endpoint."
}

if ($aggregate.systemPairMetrics.systemPairCount -ne $systemPairMetrics.systemPairCount) {
    throw "Analytics dashboard systemPairCount does not match component endpoint."
}

if ($aggregate.runStatusMetrics.totalFailureCount -ne $runStatusMetrics.totalFailureCount) {
    throw "Analytics dashboard run-status totalFailureCount does not match component endpoint."
}

if ($aggregate.runStatusMetrics.runStatusCount -ne $runStatusMetrics.runStatusCount) {
    throw "Analytics dashboard runStatusCount does not match component endpoint."
}

if ($aggregate.catalog.failureTypeCount -ne $catalog.failureTypeCount) {
    throw "Analytics dashboard catalog failureTypeCount does not match catalog endpoint."
}

if ($aggregate.catalog.runStatusCount -ne $catalog.runStatusCount) {
    throw "Analytics dashboard catalog runStatusCount does not match catalog endpoint."
}

if ($aggregate.catalog.sourceSystemCount -ne $catalog.sourceSystemCount) {
    throw "Analytics dashboard catalog sourceSystemCount does not match catalog endpoint."
}

if ($aggregate.catalog.targetSystemCount -ne $catalog.targetSystemCount) {
    throw "Analytics dashboard catalog targetSystemCount does not match catalog endpoint."
}

Write-Host "RecentFailureCount: $($aggregate.dashboard.recentFailures.count)"
Write-Host "MetricsTotalFailureCount: $($aggregate.dashboard.metrics.totalFailureCount)"
Write-Host "SystemPairCount: $($aggregate.systemPairMetrics.systemPairCount)"
Write-Host "RunStatusCount: $($aggregate.runStatusMetrics.runStatusCount)"
Write-Host "FailureTypeCount: $($aggregate.catalog.failureTypeCount)"

Write-Host ""
Write-Host "Global operational failure analytics dashboard consistency smoke passed."
