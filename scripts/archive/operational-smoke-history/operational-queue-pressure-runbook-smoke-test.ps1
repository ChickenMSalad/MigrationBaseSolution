param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

$url = "$BaseUrl/api/operational/queue-pressure/runbook"
$response = Invoke-RestMethod -Uri $url -Method Get

if ($null -eq $response.runbook) {
    throw "Missing runbook root object."
}

if ($response.runbook.totalPhaseCount -lt 4) {
    throw "Expected at least four runbook phases."
}

Write-Host "Queue pressure runbook smoke test passed."
