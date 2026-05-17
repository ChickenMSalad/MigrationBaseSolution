param(
    [string]$BaseUrl = "http://localhost:5173"
)

$ErrorActionPreference = "Stop"

Write-Host "Operational Readiness Rollups Smoke Test"
Write-Host "Base URL: $BaseUrl"
Write-Host ""

$snapshot = Invoke-RestMethod "$BaseUrl/api/cloud/operations/readiness"

Write-Host "Operationally ready       : $($snapshot.isOperationallyReady)"
Write-Host "Ready for live queue      : $($snapshot.isReadyForLiveQueueExecution)"
Write-Host "Audit provider            : $($snapshot.audit.providerKind)"
Write-Host "Telemetry provider        : $($snapshot.telemetry.providerKind)"
Write-Host "Queue receive provider    : $($snapshot.queueExecution.receiveProvider.providerKind)"
Write-Host "Blocking issues           : $($snapshot.blockingIssues.Count)"
Write-Host "Warnings                  : $($snapshot.warnings.Count)"

if ($null -eq $snapshot.audit) {
    throw "Missing audit section."
}

if ($null -eq $snapshot.telemetry) {
    throw "Missing telemetry section."
}

if ($null -eq $snapshot.queueExecution) {
    throw "Missing queue execution section."
}

Write-Host ""
Write-Host "Operational readiness rollups smoke test completed successfully."
