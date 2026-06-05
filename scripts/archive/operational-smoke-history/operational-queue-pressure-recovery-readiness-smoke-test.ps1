param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"
$url = "$BaseUrl/api/operational/queue-pressure/recovery-readiness?pressureLevel=High"

Write-Host "Calling $url"
$response = Invoke-RestMethod -Uri $url -Method Get

if ($null -eq $response.recoveryReadiness) {
    throw "Response did not contain recoveryReadiness."
}

if ($response.recoveryReadiness.isReadOnly -ne $true) {
    throw "recoveryReadiness must report read-only mode."
}

if ($null -eq $response.recoveryReadiness.summary) {
    throw "recoveryReadiness summary was missing."
}

Write-Host "Smoke test passed for /api/operational/queue-pressure/recovery-readiness"
