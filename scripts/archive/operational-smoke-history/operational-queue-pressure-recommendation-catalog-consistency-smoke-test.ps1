param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"
$url = "$BaseUrl/api/operational/queue-pressure/recommendation-catalog/readiness"
Write-Host "Calling $url"
$response = Invoke-RestMethod -Uri $url -Method Get

if ($null -eq $response.readiness) {
    throw "Response missing readiness root."
}

if ($response.readiness.isAvailable -ne $true) {
    throw "Expected recommendation catalog readiness to be available."
}

if ($response.readiness.recommendationCount -lt 1) {
    throw "Expected recommendationCount to be at least 1."
}

Write-Host "Consistency smoke test passed."
