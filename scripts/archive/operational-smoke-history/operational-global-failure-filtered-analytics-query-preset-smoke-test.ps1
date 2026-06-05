param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$Limit = 10
)

$ErrorActionPreference = "Stop"

Write-Host "Requesting failure analytics preset catalog..."
$catalog = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/analytics-presets?limit=$Limit" `
    -ContentType "application/json"

Write-Host "PresetCount: $($catalog.count)"

if ($catalog.count -lt 1) {
    throw "Expected at least one failure analytics preset."
}

$catalog | ConvertTo-Json -Depth 20

Write-Host ""
Write-Host "Requesting all-recent preset analytics..."
$allRecent = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/analytics-presets/all-recent?limit=$Limit" `
    -ContentType "application/json"

Write-Host "PresetKey: $($allRecent.preset.presetKey)"
Write-Host "ResultCount: $($allRecent.analytics.results.count)"
Write-Host "MetricsTotalFailureCount: $($allRecent.analytics.metrics.totalFailureCount)"

if ($allRecent.analytics.results.count -gt $Limit) {
    throw "Preset analytics limit was not respected."
}

Write-Host ""
Write-Host "Requesting retriable preset analytics..."
$retriable = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/analytics-presets/retriable?limit=$Limit" `
    -ContentType "application/json"

foreach ($failure in @($retriable.analytics.results.failures)) {
    if (-not $failure.isRetriable) {
        throw "Retriable preset returned a non-retriable failure."
    }
}

Write-Host "Failure analytics preset smoke passed."
