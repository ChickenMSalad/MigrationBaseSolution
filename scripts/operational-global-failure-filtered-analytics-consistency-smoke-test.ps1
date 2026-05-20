param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$Limit = 10
)

$ErrorActionPreference = "Stop"

Write-Host "Loading filtered analytics baseline..."
$baseline = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/filtered-analytics?limit=$Limit" `
    -ContentType "application/json"

if ($baseline.results.count -ne @($baseline.results.failures).Count) {
    throw "Filtered analytics results count does not match failures array length."
}

if ($baseline.metrics.totalFailureCount -ne $baseline.results.count) {
    throw "Filtered analytics metrics totalFailureCount does not match results count."
}

$failureTypeTotal = 0
foreach ($metric in @($baseline.metrics.failureTypes)) {
    $failureTypeTotal += $metric.count
}

if ($failureTypeTotal -ne $baseline.metrics.totalFailureCount) {
    throw "Filtered analytics failure type metrics do not sum to totalFailureCount."
}

$runStatusTotal = 0
foreach ($metric in @($baseline.metrics.runStatuses)) {
    $runStatusTotal += $metric.count
}

if ($runStatusTotal -ne $baseline.metrics.totalFailureCount) {
    throw "Filtered analytics run status metrics do not sum to totalFailureCount."
}

$systemPairTotal = 0
foreach ($metric in @($baseline.metrics.systemPairs)) {
    $systemPairTotal += $metric.count
}

if ($systemPairTotal -ne $baseline.metrics.totalFailureCount) {
    throw "Filtered analytics system pair metrics do not sum to totalFailureCount."
}

Write-Host ""
Write-Host "Loading retriable filtered analytics..."
$retriable = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/filtered-analytics?isRetriable=true&limit=$Limit" `
    -ContentType "application/json"

foreach ($failure in @($retriable.results.failures)) {
    if (-not $failure.isRetriable) {
        throw "Retriable filtered analytics returned a non-retriable failure."
    }
}

if ($retriable.metrics.nonRetriableFailureCount -ne 0) {
    throw "Retriable filtered analytics returned non-retriable metric counts."
}

Write-Host ""
Write-Host "Loading search filtered analytics..."
$search = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/filtered-analytics?q=Failure&limit=$Limit" `
    -ContentType "application/json"

foreach ($failure in @($search.results.failures)) {
    $haystack = "$($failure.failureId) $($failure.runId) $($failure.manifestRecordId) $($failure.workItemId) $($failure.failureType) $($failure.message) $($failure.details) $($failure.runStatus) $($failure.sourceSystem) $($failure.targetSystem) $($failure.workItemStatus)"

    if ($haystack.IndexOf("Failure", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Filtered analytics search returned a failure that does not match search text."
    }
}

Write-Host "BaselineResultCount: $($baseline.results.count)"
Write-Host "RetriableResultCount: $($retriable.results.count)"
Write-Host "SearchResultCount: $($search.results.count)"

Write-Host ""
Write-Host "Global operational failure filtered analytics consistency smoke passed."
