param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"
$url = "$BaseUrl/api/operational/queue-pressure/capacity-guardrails/readiness"

Write-Host "Consistency testing $url"
$response = Invoke-RestMethod -Uri $url -Method Get

if ($null -eq $response.readiness) {
    throw "Response did not contain readiness root object."
}

if ($response.readiness.isAvailable -ne $true) {
    throw "Capacity guardrails readiness did not report available."
}

if ($response.readiness.guardrailCount -lt 1) {
    throw "Capacity guardrails readiness did not report guardrails."
}

Write-Host "Consistency smoke test passed."
