param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"
$url = "$BaseUrl/api/operational/queue-pressure/trend/readiness"
Write-Host "Calling $url"
$response = Invoke-RestMethod -Uri $url -Method Get

if ($null -eq $response.readiness) {
    throw "Response missing readiness root."
}

if ($response.readiness.expectedSignalCount -lt 1) {
    throw "Expected at least one trend signal."
}

if ($null -eq $response.readiness.availableSignalCount) {
    throw "Response missing readiness.availableSignalCount."
}

Write-Host "Consistency smoke test passed."
