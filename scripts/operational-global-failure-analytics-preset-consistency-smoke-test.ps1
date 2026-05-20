param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$Limit = 10
)

$ErrorActionPreference = "Stop"

Write-Host "Loading preset catalog..."
$catalog = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/analytics-presets?limit=$Limit" `
    -ContentType "application/json"

if ($catalog.count -ne @($catalog.presets).Count) {
    throw "Preset catalog count does not match presets array length."
}

$expectedPresetKeys = @(
    "all-recent",
    "retriable",
    "non-retriable",
    "failed-runs",
    "work-item-failures"
)

foreach ($key in $expectedPresetKeys) {
    $preset = @($catalog.presets) | Where-Object { $_.presetKey -eq $key }

    if (-not $preset) {
        throw "Preset catalog is missing expected preset: $key"
    }
}

Write-Host "Catalog contains expected presets."

Write-Host ""
Write-Host "Comparing all-recent preset to filtered analytics endpoint..."

$presetAnalytics = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/analytics-presets/all-recent?limit=$Limit" `
    -ContentType "application/json"

$filteredAnalytics = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/filtered-analytics?limit=$Limit" `
    -ContentType "application/json"

if ($presetAnalytics.analytics.results.count -ne $filteredAnalytics.results.count) {
    throw "All-recent preset result count does not match filtered analytics."
}

if ($presetAnalytics.analytics.metrics.totalFailureCount -ne $filteredAnalytics.metrics.totalFailureCount) {
    throw "All-recent preset metric total does not match filtered analytics."
}

if ($presetAnalytics.analytics.results.count -gt $Limit) {
    throw "All-recent preset limit was not respected."
}

Write-Host ""
Write-Host "Checking retriable preset..."

$retriable = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/analytics-presets/retriable?limit=$Limit" `
    -ContentType "application/json"

foreach ($failure in @($retriable.analytics.results.failures)) {
    if (-not $failure.isRetriable) {
        throw "Retriable preset returned a non-retriable failure."
    }
}

if ($retriable.analytics.metrics.nonRetriableFailureCount -ne 0) {
    throw "Retriable preset returned non-retriable metrics."
}

Write-Host ""
Write-Host "Checking non-retriable preset..."

$nonRetriable = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/analytics-presets/non-retriable?limit=$Limit" `
    -ContentType "application/json"

foreach ($failure in @($nonRetriable.analytics.results.failures)) {
    if ($failure.isRetriable) {
        throw "Non-retriable preset returned a retriable failure."
    }
}

if ($nonRetriable.analytics.metrics.retriableFailureCount -ne 0) {
    throw "Non-retriable preset returned retriable metrics."
}

Write-Host ""
Write-Host "Checking unknown preset returns 404..."

try {
    Invoke-RestMethod `
        -Method Get `
        -Uri "$BaseUrl/api/operational/failures/analytics-presets/does-not-exist?limit=$Limit" `
        -ContentType "application/json" | Out-Null

    throw "Unknown preset unexpectedly succeeded."
}
catch [System.Net.WebException] {
    if ($_.Exception.Response.StatusCode.value__ -ne 404) {
        throw "Unknown preset returned unexpected status code: $($_.Exception.Response.StatusCode.value__)"
    }

    Write-Host "Unknown preset returned 404 as expected."
}

Write-Host "PresetCount: $($catalog.count)"
Write-Host "AllRecentResultCount: $($presetAnalytics.analytics.results.count)"
Write-Host "RetriableResultCount: $($retriable.analytics.results.count)"
Write-Host "NonRetriableResultCount: $($nonRetriable.analytics.results.count)"

Write-Host ""
Write-Host "Global operational failure analytics preset consistency smoke passed."
