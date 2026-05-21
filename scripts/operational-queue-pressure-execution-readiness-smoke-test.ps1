param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"
$url = "$BaseUrl/api/operational/queue-pressure/execution-readiness?pressureLevel=Elevated"

Write-Host "Calling $url"
$response = Invoke-RestMethod -Uri $url -Method Get

if ($null -eq $response.executionReadiness) {
    throw "Response did not contain executionReadiness."
}

if ($response.executionReadiness.isReadOnly -ne $true) {
    throw "executionReadiness must report read-only mode."
}

if ($null -eq $response.executionReadiness.summary) {
    throw "executionReadiness summary was missing."
}

Write-Host "Smoke test passed for /api/operational/queue-pressure/execution-readiness"
