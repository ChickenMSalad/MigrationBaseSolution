param(
    [string]$BaseUrl = "http://localhost:5173"
)

$ErrorActionPreference = "Stop"

Write-Host "Production Safety Gates Smoke Test"
Write-Host "Base URL: $BaseUrl"
Write-Host ""

$snapshot = Invoke-RestMethod "$BaseUrl/api/cloud/operations/production-safety-gates"

Write-Host "Production ready             : $($snapshot.isProductionReady)"
Write-Host "Live queue execution allowed : $($snapshot.isLiveQueueExecutionAllowed)"
Write-Host "Gate count                   : $($snapshot.gates.Count)"
Write-Host "Blocking issues              : $($snapshot.blockingIssues.Count)"
Write-Host "Warnings                     : $($snapshot.warnings.Count)"

if ($snapshot.gates.Count -lt 3) {
    throw "Expected production safety gates."
}

if ($null -eq $snapshot.authPolicy) {
    throw "Missing auth policy readiness section."
}

if ($null -eq $snapshot.credentialAccess) {
    throw "Missing credential access policy section."
}

if ($null -eq $snapshot.operationalReadiness) {
    throw "Missing operational readiness section."
}

Write-Host ""
Write-Host "Production safety gates smoke test completed successfully."
