param(
    [string]$BaseUrl = "http://localhost:5173"
)

$ErrorActionPreference = "Stop"

Write-Host "Queue Execution Governance Smoke Test"
Write-Host "Base URL: $BaseUrl"
Write-Host ""

$decision = Invoke-RestMethod "$BaseUrl/api/cloud/operations/queue-execution-governance"

Write-Host "Can enable live queue : $($decision.canEnableLiveQueueExecution)"
Write-Host "Can complete messages : $($decision.canCompleteMessages)"
Write-Host "Requires approval     : $($decision.requiresManualApproval)"
Write-Host "Recommended mode      : $($decision.recommendedMode)"
Write-Host "Required conditions   : $($decision.requiredConditions.Count)"
Write-Host "Blocking issues       : $($decision.blockingIssues.Count)"
Write-Host "Warnings              : $($decision.warnings.Count)"

if ($decision.requiredConditions.Count -lt 3) {
    throw "Expected queue execution governance required conditions."
}

if ($decision.requiresManualApproval -ne $true) {
    throw "Expected queue execution governance to require manual approval."
}

Write-Host ""
Write-Host "Queue execution governance smoke test completed successfully."
