param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$Limit = 10
)

$ErrorActionPreference = "Stop"

Write-Host "Loading preset catalog..."
$presetCatalog = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/analytics-presets?limit=$Limit" `
    -ContentType "application/json"

$presetKeys = @{}
foreach ($preset in @($presetCatalog.presets)) {
    $presetKeys[$preset.presetKey] = $true
}

Write-Host "Loading favorite catalog..."
$favorites = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/analytics-preset-favorites" `
    -ContentType "application/json"

if ($favorites.count -ne @($favorites.favorites).Count) {
    throw "Favorites count does not match favorites array length."
}

foreach ($favorite in @($favorites.favorites)) {
    if (-not $favorite.favoriteKey) {
        throw "Favorite is missing favoriteKey."
    }

    if (-not $favorite.displayName) {
        throw "Favorite is missing displayName."
    }

    if (@($favorite.presetKeys).Count -lt 1) {
        throw "Favorite has no preset keys: $($favorite.favoriteKey)"
    }

    foreach ($presetKey in @($favorite.presetKeys)) {
        if (-not $presetKeys.ContainsKey($presetKey)) {
            throw "Favorite '$($favorite.favoriteKey)' references unknown preset '$presetKey'."
        }
    }
}

Write-Host "Favorite catalog references valid presets."

foreach ($favorite in @($favorites.favorites)) {
    Write-Host ""
    Write-Host "Checking favorite dashboard: $($favorite.favoriteKey)"

    $dashboard = Invoke-RestMethod `
        -Method Get `
        -Uri "$BaseUrl/api/operational/failures/analytics-preset-favorites/$($favorite.favoriteKey)?limit=$Limit" `
        -ContentType "application/json"

    if ($dashboard.favorite.favoriteKey -ne $favorite.favoriteKey) {
        throw "Favorite dashboard returned wrong favorite key for $($favorite.favoriteKey)."
    }

    if ($dashboard.count -ne @($dashboard.presets).Count) {
        throw "Favorite dashboard count does not match presets array length for $($favorite.favoriteKey)."
    }

    if ($dashboard.count -ne @($favorite.presetKeys).Count) {
        throw "Favorite dashboard preset count does not match favorite preset key count for $($favorite.favoriteKey)."
    }

    foreach ($presetAnalytics in @($dashboard.presets)) {
        if (-not (@($favorite.presetKeys) -contains $presetAnalytics.preset.presetKey)) {
            throw "Favorite dashboard returned preset not in favorite definition: $($presetAnalytics.preset.presetKey)."
        }

        if ($presetAnalytics.analytics.results.count -gt $Limit) {
            throw "Favorite dashboard preset result limit was not respected."
        }
    }
}

Write-Host ""
Write-Host "Checking unknown favorite returns 404..."

try {
    Invoke-RestMethod `
        -Method Get `
        -Uri "$BaseUrl/api/operational/failures/analytics-preset-favorites/does-not-exist?limit=$Limit" `
        -ContentType "application/json" | Out-Null

    throw "Unknown favorite unexpectedly succeeded."
}
catch [System.Net.WebException] {
    if ($_.Exception.Response.StatusCode.value__ -ne 404) {
        throw "Unknown favorite returned unexpected status code: $($_.Exception.Response.StatusCode.value__)"
    }

    Write-Host "Unknown favorite returned 404 as expected."
}

Write-Host "FavoriteCount: $($favorites.count)"
Write-Host ""
Write-Host "Global operational failure analytics preset favorites consistency smoke passed."
