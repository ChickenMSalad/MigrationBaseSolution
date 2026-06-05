param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$SampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "Loading recent failures sample..."
$recent = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/recent?limit=$SampleLimit" `
    -ContentType "application/json"

Write-Host "Loading run-status metrics..."
$metrics = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/run-status-metrics?sampleLimit=$SampleLimit" `
    -ContentType "application/json"

$failures = @($recent.failures)

if ($metrics.totalFailureCount -ne $failures.Count) {
    throw "Run-status metrics totalFailureCount does not match recent failures sample."
}

$expectedStatuses = @{}

foreach ($failure in $failures) {
    $status = if ($failure.runStatus) { $failure.runStatus } else { "" }

    if (-not $expectedStatuses.ContainsKey($status)) {
        $expectedStatuses[$status] = [pscustomobject]@{
            RunStatus = $status
            Count = 0
            RetriableCount = 0
            NonRetriableCount = 0
            FailureTypes = @{}
        }
    }

    $expectedStatuses[$status].Count++

    if ($failure.isRetriable) {
        $expectedStatuses[$status].RetriableCount++
    }
    else {
        $expectedStatuses[$status].NonRetriableCount++
    }

    $failureType = if ($failure.failureType) { $failure.failureType } else { "" }
    if (-not $expectedStatuses[$status].FailureTypes.ContainsKey($failureType)) {
        $expectedStatuses[$status].FailureTypes[$failureType] = 0
    }

    $expectedStatuses[$status].FailureTypes[$failureType]++
}

if ($metrics.runStatusCount -ne $expectedStatuses.Count) {
    throw "Run-status metrics runStatusCount does not match expected status count."
}

foreach ($statusMetric in @($metrics.runStatuses)) {
    $status = if ($statusMetric.runStatus) { $statusMetric.runStatus } else { "" }

    if (-not $expectedStatuses.ContainsKey($status)) {
        throw "Run-status metrics returned unexpected status: $status"
    }

    $expected = $expectedStatuses[$status]

    if ($statusMetric.count -ne $expected.Count) {
        throw "Run-status count mismatch for $status."
    }

    if ($statusMetric.retriableCount -ne $expected.RetriableCount) {
        throw "Run-status retriable count mismatch for $status."
    }

    if ($statusMetric.nonRetriableCount -ne $expected.NonRetriableCount) {
        throw "Run-status non-retriable count mismatch for $status."
    }

    $typeTotal = 0
    foreach ($typeMetric in @($statusMetric.failureTypes)) {
        $typeTotal += $typeMetric.count

        $failureType = if ($typeMetric.failureType) { $typeMetric.failureType } else { "" }
        if (-not $expected.FailureTypes.ContainsKey($failureType)) {
            throw "Run-status metric returned unexpected failure type '$failureType' for status '$status'."
        }

        if ($expected.FailureTypes[$failureType] -ne $typeMetric.count) {
            throw "Run-status failure type count mismatch for '$failureType' in status '$status'."
        }
    }

    if ($typeTotal -ne $statusMetric.count) {
        throw "Run-status failure type breakdown does not sum to status count for $status."
    }
}

Write-Host "TotalFailureCount: $($metrics.totalFailureCount)"
Write-Host "RunStatusCount: $($metrics.runStatusCount)"
Write-Host ""
Write-Host "Global operational failure run-status metrics consistency smoke passed."
