param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$SampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "Loading recent failures sample..."
$failures = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/recent?limit=$SampleLimit" `
    -ContentType "application/json"

Write-Host "Loading failure metrics..."
$metrics = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/metrics?sampleLimit=$SampleLimit" `
    -ContentType "application/json"

$failureItems = @($failures.failures)

if ($metrics.totalFailureCount -ne $failureItems.Count) {
    throw "Metrics totalFailureCount does not match recent failures count."
}

$expectedRetriable = @($failureItems | Where-Object { $_.isRetriable }).Count
$expectedNonRetriable = @($failureItems | Where-Object { -not $_.isRetriable }).Count

if ($metrics.retriableFailureCount -ne $expectedRetriable) {
    throw "Metrics retriableFailureCount does not match failures sample."
}

if ($metrics.nonRetriableFailureCount -ne $expectedNonRetriable) {
    throw "Metrics nonRetriableFailureCount does not match failures sample."
}

if ($failureItems.Count -gt 0) {
    $expectedFirst = ($failureItems | Sort-Object createdAt | Select-Object -First 1).createdAt
    $expectedLast = ($failureItems | Sort-Object createdAt -Descending | Select-Object -First 1).createdAt

    if ($metrics.firstFailureAt -ne $expectedFirst) {
        throw "Metrics firstFailureAt does not match failures sample."
    }

    if ($metrics.lastFailureAt -ne $expectedLast) {
        throw "Metrics lastFailureAt does not match failures sample."
    }
}

$failureTypeCounts = @{}

foreach ($failure in $failureItems) {
    if (-not $failureTypeCounts.ContainsKey($failure.failureType)) {
        $failureTypeCounts[$failure.failureType] = 0
    }

    $failureTypeCounts[$failure.failureType]++
}

foreach ($metric in @($metrics.failureTypes)) {
    if (-not $failureTypeCounts.ContainsKey($metric.failureType)) {
        throw "Metrics returned unknown failure type: $($metric.failureType)"
    }

    if ($failureTypeCounts[$metric.failureType] -ne $metric.count) {
        throw "Failure type metric mismatch for $($metric.failureType)"
    }
}

Write-Host "TotalFailureCount: $($metrics.totalFailureCount)"
Write-Host "FailureTypeMetricCount: $(@($metrics.failureTypes).Count)"
Write-Host "RunStatusMetricCount: $(@($metrics.runStatuses).Count)"
Write-Host "SystemPairMetricCount: $(@($metrics.systemPairs).Count)"

Write-Host ""
Write-Host "Global operational failure metrics consistency smoke passed."
