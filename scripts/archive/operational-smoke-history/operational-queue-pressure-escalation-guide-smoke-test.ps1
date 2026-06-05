param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

$url = "$BaseUrl/api/operational/queue-pressure/escalation-guide"
$response = Invoke-RestMethod -Uri $url -Method Get

if ($null -eq $response.escalationGuide) {
    throw "Missing escalationGuide root object."
}

if ($response.escalationGuide.totalEscalationLevelCount -lt 1) {
    throw "Expected at least one escalation guide item."
}

Write-Host "Queue pressure escalation-guide smoke test passed."
