param(
    [string]$BaseUrl = "http://localhost:5173"
)

$ErrorActionPreference = "Stop"

Write-Host "Queue Execution Observability Smoke Test"
Write-Host "Base URL: $BaseUrl"
Write-Host ""

$snapshot = Invoke-RestMethod `
    "$BaseUrl/api/cloud/queue/execution-observability"

Write-Host "Provider       : $($snapshot.providerKind)"
Write-Host "Queue          : $($snapshot.queueName)"
Write-Host "Configured     : $($snapshot.receiveProviderConfigured)"
Write-Host "Worker Enabled : $($snapshot.workerLoopEnabled)"
Write-Host "Coordinator DR : $($snapshot.coordinatorDryRun)"
Write-Host "Max Messages   : $($snapshot.maxMessages)"
Write-Host "Warnings       : $($snapshot.warnings.Count)"

if ($snapshot.supportedMessageTypes.Count -lt 1) {
    throw "Expected supported message types."
}

Write-Host ""
Write-Host "Queue execution observability smoke test completed successfully."
