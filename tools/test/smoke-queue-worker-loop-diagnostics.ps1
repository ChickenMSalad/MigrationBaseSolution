param(
    [string]$BaseUrl = "http://localhost:5173"
)

$ErrorActionPreference = "Stop"

Write-Host "Queue Worker Loop Diagnostics Smoke Test"
Write-Host "Base URL: $BaseUrl"
Write-Host ""

$plan = Invoke-RestMethod "$BaseUrl/api/cloud/queue/worker-loop"

Write-Host "Enabled      : $($plan.enabled)"
Write-Host "DryRun       : $($plan.dryRun)"
Write-Host "Provider     : $($plan.receiveProviderKind)"
Write-Host "Queue        : $($plan.logicalQueueName)"
Write-Host "Configured   : $($plan.receiveProviderConfigured)"

if ($null -eq $plan.warnings) {
    throw "Expected warnings array on worker loop plan."
}

$safety = Invoke-RestMethod "$BaseUrl/api/cloud/queue/worker-loop/safety"

if ($null -eq $safety.descriptor) {
    throw "Worker loop safety response did not include descriptor."
}

Write-Host "CanRun       : $($safety.canRun)"
Write-Host "SafeToStart  : $($safety.safeToStart)"
Write-Host ""

Write-Host "Queue worker loop diagnostics smoke test completed successfully."
