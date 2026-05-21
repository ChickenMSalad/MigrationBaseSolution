param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"
$url = "$BaseUrl/api/operational/queue-pressure/throttle-policy?pressureLevel=Elevated"

Write-Host "Calling $url"
$response = Invoke-RestMethod -Uri $url -Method Get

if ($null -eq $response.throttlePolicy) {
    throw "Response did not contain throttlePolicy."
}

if ($null -eq $response.throttlePolicy.selectedPolicy) {
    throw "Response did not contain selectedPolicy."
}

Write-Host "Smoke test passed for /api/operational/queue-pressure/throttle-policy"
