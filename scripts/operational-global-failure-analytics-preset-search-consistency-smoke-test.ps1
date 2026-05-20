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

$catalogKeys = @{}
foreach ($preset in @($catalog.presets)) {
    $catalogKeys[$preset.presetKey] = $true
}

Write-Host "Loading empty preset search..."
$allSearch = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/analytics-presets/search?limit=$Limit" `
    -ContentType "application/json"

if ($allSearch.count -ne @($allSearch.presets).Count) {
    throw "Empty preset search count does not match presets array length."
}

if ($allSearch.count -gt $Limit) {
    throw "Empty preset search limit was not respected."
}

foreach ($preset in @($allSearch.presets)) {
    if (-not $catalogKeys.ContainsKey($preset.presetKey)) {
        throw "Preset search returned preset not found in catalog: $($preset.presetKey)"
    }
}

Write-Host "Loading retriable preset search..."
$retriableSearch = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/analytics-presets/search?q=retriable&limit=$Limit" `
    -ContentType "application/json"

if ($retriableSearch.searchText -ne "retriable") {
    throw "Preset searchText does not match requested text."
}

foreach ($preset in @($retriableSearch.presets)) {
    if (-not $catalogKeys.ContainsKey($preset.presetKey)) {
        throw "Retriable preset search returned preset not found in catalog: $($preset.presetKey)"
    }

    $haystack = "$($preset.presetKey) $($preset.displayName) $($preset.description) $($preset.query.failureType) $($preset.query.sourceSystem) $($preset.query.targetSystem) $($preset.query.searchText) $($preset.query.isRetriable)"

    if ($haystack.IndexOf("retriable", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Retriable preset search returned a result that does not match search text."
    }
}

Write-Host "Loading no-match preset search..."
$noMatch = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/analytics-presets/search?q=definitely-no-such-preset-xyz&limit=$Limit" `
    -ContentType "application/json"

if ($noMatch.count -ne 0) {
    throw "No-match preset search returned unexpected results."
}

Write-Host "CatalogCount: $($catalog.count)"
Write-Host "EmptySearchCount: $($allSearch.count)"
Write-Host "RetriableSearchCount: $($retriableSearch.count)"
Write-Host "NoMatchSearchCount: $($noMatch.count)"

Write-Host ""
Write-Host "Global operational failure analytics preset search consistency smoke passed."
