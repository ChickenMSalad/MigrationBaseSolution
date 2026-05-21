param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"
$url = "$BaseUrl/api/operational/queue-pressure/capacity-guardrails"

Write-Host "Smoke testing $url"
$response = Invoke-RestMethod -Uri $url -Method Get

if ($null -eq $response.capacityGuardrails) {
    throw "Response did not contain capacityGuardrails root object."
}

if ($null -eq $response.capacityGuardrails.guardrails -or $response.capacityGuardrails.guardrails.Count -lt 1) {
    throw "Response did not contain guardrails."
}

Write-Host "Smoke test passed."
