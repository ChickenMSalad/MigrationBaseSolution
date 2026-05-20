param(
    [string]$BaseUrl = "https://localhost:55436",
    [string]$PresetKey = "all-recent",
    [int]$Limit = 10
)

$ErrorActionPreference = "Stop"

Write-Host "Loading preset catalog..."
$catalog = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/analytics-presets?limit=$Limit" `
    -ContentType "application/json"

Write-Host "Loading selected preset analytics..."
$preset = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/analytics-presets/$PresetKey`?limit=$Limit" `
    -ContentType "application/json"

Write-Host "Loading preset dashboard aggregate..."
$dashboard = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/analytics-preset-dashboard?presetKey=$PresetKey&limit=$Limit" `
    -ContentType "application/json"

if ($dashboard.catalog.count -ne $catalog.count) {
    throw "Preset dashboard catalog count does not match catalog endpoint."
}

if ($dashboard.selectedPreset.preset.presetKey -ne $preset.preset.presetKey) {
    throw "Preset dashboard selected preset key does not match selected preset endpoint."
}

if ($dashboard.selectedPreset.analytics.results.count -ne $preset.analytics.results.count) {
    throw "Preset dashboard selected result count does not match selected preset endpoint."
}

if ($dashboard.selectedPreset.analytics.metrics.totalFailureCount -ne $preset.analytics.metrics.totalFailureCount) {
    throw "Preset dashboard selected metrics total does not match selected preset endpoint."
}

if ($dashboard.selectedPreset.analytics.results.count -gt $Limit) {
    throw "Preset dashboard selected result limit was not respected."
}

Write-Host "CatalogCount: $($dashboard.catalog.count)"
Write-Host "SelectedPresetKey: $($dashboard.selectedPreset.preset.presetKey)"
Write-Host "SelectedResultCount: $($dashboard.selectedPreset.analytics.results.count)"
Write-Host "SelectedMetricsTotalFailureCount: $($dashboard.selectedPreset.analytics.metrics.totalFailureCount)"

Write-Host ""
Write-Host "Checking retriable preset dashboard consistency..."

$retriableDashboard = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/analytics-preset-dashboard?presetKey=retriable&limit=$Limit" `
    -ContentType "application/json"

foreach ($failure in @($retriableDashboard.selectedPreset.analytics.results.failures)) {
    if (-not $failure.isRetriable) {
        throw "Retriable preset dashboard returned a non-retriable failure."
    }
}

if ($retriableDashboard.selectedPreset.analytics.metrics.nonRetriableFailureCount -ne 0) {
    throw "Retriable preset dashboard returned non-retriable metrics."
}

Write-Host ""
Write-Host "Checking unknown preset dashboard returns 404..."

try {
    Invoke-RestMethod `
        -Method Get `
        -Uri "$BaseUrl/api/operational/failures/analytics-preset-dashboard?presetKey=does-not-exist&limit=$Limit" `
        -ContentType "application/json" | Out-Null

    throw "Unknown preset dashboard unexpectedly succeeded."
}
catch [System.Net.WebException] {
    if ($_.Exception.Response.StatusCode.value__ -ne 404) {
        throw "Unknown preset dashboard returned unexpected status code: $($_.Exception.Response.StatusCode.value__)"
    }

    Write-Host "Unknown preset dashboard returned 404 as expected."
}

Write-Host ""
Write-Host "Global operational failure analytics preset dashboard consistency smoke passed."
