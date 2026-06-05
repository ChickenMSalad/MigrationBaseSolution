param(
    [string]$BaseUrl = "https://localhost:55436",
    [string]$PresetKey = "all-recent",
    [int]$Limit = 10
)

$ErrorActionPreference = "Stop"

Write-Host "Requesting failure analytics preset dashboard..."
Write-Host "GET $BaseUrl/api/operational/failures/analytics-preset-dashboard?presetKey=$PresetKey&limit=$Limit"

$response = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/analytics-preset-dashboard?presetKey=$PresetKey&limit=$Limit" `
    -ContentType "application/json"

Write-Host "PresetCatalogCount: $($response.catalog.count)"
Write-Host "SelectedPresetKey: $($response.selectedPreset.preset.presetKey)"
Write-Host "SelectedResultCount: $($response.selectedPreset.analytics.results.count)"
Write-Host "SelectedMetricsTotalFailureCount: $($response.selectedPreset.analytics.metrics.totalFailureCount)"
Write-Host "GeneratedAt: $($response.generatedAt)"

if ($response.selectedPreset.analytics.results.count -gt $Limit) {
    throw "Preset dashboard selected result limit was not respected."
}

if ($response.selectedPreset.preset.presetKey -ne $PresetKey) {
    throw "Preset dashboard selected preset key did not match requested preset key."
}

if ($response.messages) {
    Write-Host ""
    Write-Host "Messages:"
    foreach ($message in $response.messages) {
        Write-Host "- $message"
    }
}

$response | ConvertTo-Json -Depth 30
