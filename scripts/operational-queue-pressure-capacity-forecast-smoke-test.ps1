param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"
$url = "$BaseUrl/api/operational/queue-pressure/capacity-forecast?horizonHours=24"

Write-Host "Smoke testing $url"
$response = Invoke-RestMethod -Uri $url -Method Get

if ($null -eq $response.capacityForecast) {
    throw "Response did not contain capacityForecast root object."
}

if ($null -eq $response.capacityForecast.forecastBands -or $response.capacityForecast.forecastBands.Count -lt 1) {
    throw "Response did not contain forecastBands."
}

Write-Host "Smoke test passed."
