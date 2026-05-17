param(
    [string]$BaseUrl = "http://localhost:5173"
)

$ErrorActionPreference = "Stop"

Write-Host "Queue Execution Readiness Smoke Test"
Write-Host "Base URL: $BaseUrl"
Write-Host ""

$snapshot = Invoke-RestMethod "$BaseUrl/api/cloud/queue/execution-readiness"

Write-Host "Ready for dry-run : $($snapshot.isReadyForDryRun)"
Write-Host "Ready for live    : $($snapshot.isReadyForLiveExecution)"
Write-Host "Dispatch provider : $($snapshot.dispatchProvider.providerKind)"
Write-Host "Receive provider  : $($snapshot.receiveProvider.providerKind)"
Write-Host "Blocking issues   : $($snapshot.blockingIssues.Count)"
Write-Host "Warnings          : $($snapshot.warnings.Count)"

if ($null -eq $snapshot.dispatchProvider) {
    throw "Missing dispatch provider section."
}

if ($null -eq $snapshot.receiveProvider) {
    throw "Missing receive provider section."
}

if ($null -eq $snapshot.observability) {
    throw "Missing observability section."
}

Write-Host ""
Write-Host "Queue execution readiness smoke test completed successfully."
