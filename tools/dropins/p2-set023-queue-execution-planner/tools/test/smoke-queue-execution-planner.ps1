param(
    [string]$BaseUrl = "http://localhost:5173"
)

$ErrorActionPreference = "Stop"

Write-Host "Queue Execution Planner Smoke Test"
Write-Host "Base URL: $BaseUrl"
Write-Host ""

$types = Invoke-RestMethod "$BaseUrl/api/cloud/queue/execution-plan/message-types"

if ($types.supportedMessageTypes.Count -lt 1) {
    throw "Expected supported message types."
}

Write-Host "Supported message types: $($types.supportedMessageTypes -join ', ')"

$probe = Invoke-RestMethod -Method Post "$BaseUrl/api/cloud/queue/execution-plan/probe"

Write-Host "Action    : $($probe.plan.action)"
Write-Host "CanExecute: $($probe.plan.canExecute)"

if ($probe.plan.canExecute -ne $true) {
    throw "Expected sample execution plan to be executable."
}

Write-Host ""
Write-Host "Queue execution planner smoke test completed successfully."
