param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"
$url = "$BaseUrl/api/operational/queue-pressure/action-plan?sampleLimit=25"
Write-Host "Calling $url"
$response = Invoke-RestMethod -Uri $url -Method Get

if ($null -eq $response.actionPlan) {
    throw "Response missing actionPlan root."
}

if ($null -eq $response.actionPlan.generatedAtUtc) {
    throw "Response missing actionPlan.generatedAtUtc."
}

if ($null -eq $response.actionPlan.recommendedActions) {
    throw "Response missing actionPlan.recommendedActions."
}

if ($response.actionPlan.recommendedActions.Count -lt 1) {
    throw "Expected at least one recommended action."
}

Write-Host "Smoke test passed."
