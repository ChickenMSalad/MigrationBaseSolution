param(
    [string]$BaseUrl = "https://localhost:55436",
    [string]$FavoriteKey = "triage",
    [int]$Limit = 10
)

$ErrorActionPreference = "Stop"

Write-Host "Requesting failure analytics preset favorites..."
$favorites = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/analytics-preset-favorites" `
    -ContentType "application/json"

Write-Host "FavoriteCount: $($favorites.count)"

if ($favorites.count -lt 1) {
    throw "Expected at least one failure analytics preset favorite."
}

$favorites | ConvertTo-Json -Depth 20

Write-Host ""
Write-Host "Requesting favorite dashboard for '$FavoriteKey'..."
$dashboard = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/analytics-preset-favorites/$FavoriteKey`?limit=$Limit" `
    -ContentType "application/json"

Write-Host "FavoriteKey: $($dashboard.favorite.favoriteKey)"
Write-Host "PresetResultCount: $($dashboard.count)"
Write-Host "GeneratedAt: $($dashboard.generatedAt)"

if ($dashboard.favorite.favoriteKey -ne $FavoriteKey) {
    throw "Favorite dashboard returned the wrong favorite key."
}

if ($dashboard.count -lt 1) {
    throw "Favorite dashboard returned no preset analytics."
}

$dashboard | ConvertTo-Json -Depth 30

Write-Host "Failure analytics preset favorites smoke passed."
