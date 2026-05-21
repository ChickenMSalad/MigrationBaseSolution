param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"
$url = "$BaseUrl/api/operational/queue-pressure/operator-checklist"
$response = Invoke-RestMethod -Uri $url -Method Get

if ($null -eq $response.operatorChecklist) {
    throw "Response did not include operatorChecklist."
}

if ($null -eq $response.operatorChecklist.generatedAtUtc) {
    throw "Response did not include operatorChecklist.generatedAtUtc."
}

if ($response.operatorChecklist.totalChecklistItemCount -lt 1) {
    throw "Expected at least one checklist item."
}

Write-Host "Queue pressure operator-checklist smoke test passed."
