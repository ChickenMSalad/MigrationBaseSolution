param(
    [string]$BaseUrl = "http://localhost:5173"
)

$ErrorActionPreference = "Stop"

Write-Host "Queue Executor Coordinator Smoke Test"
Write-Host "Base URL: $BaseUrl"
Write-Host ""

$options = Invoke-RestMethod "$BaseUrl/api/cloud/queue/executor-coordinator/options"

Write-Host "DryRun              : $($options.dryRun)"
Write-Host "CompleteMessages    : $($options.completeMessages)"
Write-Host "WriteFailureArtifacts: $($options.writeFailureArtifacts)"
Write-Host "MaxMessages         : $($options.maxMessages)"

$probe = Invoke-RestMethod -Method Post "$BaseUrl/api/cloud/queue/executor-coordinator/probe"

if ($probe.options.dryRun -ne $true) {
    throw "Coordinator probe should force dryRun=true."
}

if ($probe.options.completeMessages -ne $false) {
    throw "Coordinator probe should force completeMessages=false."
}

Write-Host "Received : $($probe.result.receivedCount)"
Write-Host "Planned  : $($probe.result.plannedCount)"
Write-Host "Warnings : $($probe.result.warnings.Count)"
Write-Host ""
Write-Host "Queue executor coordinator smoke test completed successfully."
