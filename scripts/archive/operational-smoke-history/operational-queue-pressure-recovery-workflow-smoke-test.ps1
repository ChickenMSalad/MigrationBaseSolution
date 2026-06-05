param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

$url = "$BaseUrl/api/operational/queue-pressure/recovery-workflow"
$response = Invoke-RestMethod -Uri $url -Method Get

if ($null -eq $response.recoveryWorkflow) {
    throw "Missing recoveryWorkflow root object."
}

if ($response.recoveryWorkflow.totalStageCount -lt 4) {
    throw "Expected at least four recovery workflow stages."
}

Write-Host "Queue pressure recovery workflow smoke test passed."
