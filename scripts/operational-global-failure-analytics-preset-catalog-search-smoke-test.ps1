param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$Limit = 10
)

$ErrorActionPreference = "Stop"

Write-Host "Searching failure analytics presets for 'retriable'..."
$retriable = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/analytics-presets/search?q=retriable&limit=$Limit" `
    -ContentType "application/json"

Write-Host "SearchText: $($retriable.searchText)"
Write-Host "Count: $($retriable.count)"

if ($retriable.count -lt 1) {
    throw "Expected at least one retriable preset search result."
}

foreach ($preset in @($retriable.presets)) {
    $haystack = "$($preset.presetKey) $($preset.displayName) $($preset.description) $($preset.query.failureType) $($preset.query.sourceSystem) $($preset.query.targetSystem) $($preset.query.searchText) $($preset.query.isRetriable)"

    if ($haystack.IndexOf("retriable", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Preset search returned a result that does not match search text."
    }
}

$retriable | ConvertTo-Json -Depth 20

Write-Host ""
Write-Host "Searching failure analytics presets with empty query..."
$all = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/analytics-presets/search?limit=$Limit" `
    -ContentType "application/json"

Write-Host "Count: $($all.count)"

if ($all.count -lt 1) {
    throw "Expected empty preset search to return presets."
}

Write-Host "Failure analytics preset catalog search smoke passed."
