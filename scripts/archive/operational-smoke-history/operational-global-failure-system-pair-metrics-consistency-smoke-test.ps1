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

Write-Host "Loading system-pair metrics..."
$metrics = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/system-pair-metrics?sampleLimit=$SampleLimit" `
    -ContentType "application/json"

$failures = @($recent.failures)

if ($metrics.totalFailureCount -ne $failures.Count) {
    throw "System-pair metrics totalFailureCount does not match recent failures sample."
}

$expectedPairs = @{}

foreach ($failure in $failures) {
    $source = if ($failure.sourceSystem) { $failure.sourceSystem } else { "" }
    $target = if ($failure.targetSystem) { $failure.targetSystem } else { "" }
    $key = "$source|$target"

    if (-not $expectedPairs.ContainsKey($key)) {
        $expectedPairs[$key] = [pscustomobject]@{
            SourceSystem = $source
            TargetSystem = $target
            Count = 0
            RetriableCount = 0
            NonRetriableCount = 0
        }
    }

    $expectedPairs[$key].Count++

    if ($failure.isRetriable) {
        $expectedPairs[$key].RetriableCount++
    }
    else {
        $expectedPairs[$key].NonRetriableCount++
    }
}

if ($metrics.systemPairCount -ne $expectedPairs.Count) {
    throw "System-pair metrics systemPairCount does not match expected pair count."
}

foreach ($pair in @($metrics.systemPairs)) {
    $key = "$($pair.sourceSystem)|$($pair.targetSystem)"

    if (-not $expectedPairs.ContainsKey($key)) {
        throw "System-pair metrics returned unexpected pair: $key"
    }

    $expected = $expectedPairs[$key]

    if ($pair.count -ne $expected.Count) {
        throw "System-pair count mismatch for $key."
    }

    if ($pair.retriableCount -ne $expected.RetriableCount) {
        throw "System-pair retriable count mismatch for $key."
    }

    if ($pair.nonRetriableCount -ne $expected.NonRetriableCount) {
        throw "System-pair non-retriable count mismatch for $key."
    }

    $typeTotal = 0
    foreach ($typeMetric in @($pair.failureTypes)) {
        $typeTotal += $typeMetric.count
    }

    if ($typeTotal -ne $pair.count) {
        throw "System-pair failure type breakdown does not sum to pair count for $key."
    }
}

Write-Host "TotalFailureCount: $($metrics.totalFailureCount)"
Write-Host "SystemPairCount: $($metrics.systemPairCount)"
Write-Host ""
Write-Host "Global operational failure system-pair metrics consistency smoke passed."
