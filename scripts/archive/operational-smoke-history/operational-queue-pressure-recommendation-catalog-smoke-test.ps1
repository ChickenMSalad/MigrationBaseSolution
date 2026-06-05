param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"
$url = "$BaseUrl/api/operational/queue-pressure/recommendation-catalog"
Write-Host "Calling $url"
$response = Invoke-RestMethod -Uri $url -Method Get

if ($null -eq $response.recommendationCatalog) {
    throw "Response missing recommendationCatalog root."
}

if ($null -eq $response.recommendationCatalog.generatedAtUtc) {
    throw "Response missing recommendationCatalog.generatedAtUtc."
}

if ($null -eq $response.recommendationCatalog.recommendations) {
    throw "Response missing recommendationCatalog.recommendations."
}

if ($response.recommendationCatalog.recommendations.Count -lt 1) {
    throw "Expected at least one recommendation."
}

Write-Host "Smoke test passed."
