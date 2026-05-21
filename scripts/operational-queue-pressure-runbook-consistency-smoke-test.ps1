param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

$url = "$BaseUrl/api/operational/queue-pressure/runbook/readiness"
$response = Invoke-RestMethod -Uri $url -Method Get

if ($null -eq $response.readiness) {
    throw "Missing readiness root object."
}

if ($response.readiness.isAvailable -ne $true) {
    throw "Runbook readiness is not available."
}

if ($response.readiness.endpoint -ne "/api/operational/queue-pressure/runbook") {
    throw "Unexpected readiness endpoint value."
}

Write-Host "Queue pressure runbook consistency smoke test passed."
