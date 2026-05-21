param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"
$url = "$BaseUrl/api/operational/queue-pressure/trend?sampleLimit=25"
Write-Host "Calling $url"
$response = Invoke-RestMethod -Uri $url -Method Get

if ($null -eq $response.trend) {
    throw "Response missing trend root."
}

if ($null -eq $response.trend.generatedAtUtc) {
    throw "Response missing trend.generatedAtUtc."
}

if ($null -eq $response.trend.signals) {
    throw "Response missing trend.signals."
}

Write-Host "Smoke test passed."
