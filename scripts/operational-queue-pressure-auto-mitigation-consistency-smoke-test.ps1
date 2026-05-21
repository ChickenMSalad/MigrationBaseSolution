param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"
$url = "$BaseUrl/api/operational/queue-pressure/auto-mitigation/readiness"

Write-Host "Calling $url"
$response = Invoke-RestMethod -Uri $url -Method Get

if ($null -eq $response.readiness) {
    throw "Response did not contain readiness."
}

if ($response.readiness.isAvailable -ne $true) {
    throw "Readiness did not report available."
}

if ($response.readiness.isAutomaticMutationEnabled -ne $false) {
    throw "Readiness must report automatic mutation disabled."
}

Write-Host "Consistency smoke test passed for /api/operational/queue-pressure/auto-mitigation"
