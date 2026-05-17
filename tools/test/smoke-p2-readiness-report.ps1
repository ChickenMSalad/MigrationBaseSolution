param(
    [string]$BaseUrl = "http://localhost:5173"
)

$ErrorActionPreference = "Stop"

Write-Host "P2 Readiness Report Smoke Test"
Write-Host "Base URL: $BaseUrl"
Write-Host ""

$snapshot = Invoke-RestMethod "$BaseUrl/api/cloud/operations/p2-readiness-report"

Write-Host "Overall status      : $($snapshot.overallStatus)"
Write-Host "Diagnostics ready   : $($snapshot.isDiagnosticsReady)"
Write-Host "Production ready    : $($snapshot.isProductionReady)"
Write-Host "Live queue ready    : $($snapshot.isLiveQueueExecutionReady)"
Write-Host "Operational mode    : $($snapshot.operationalMode)"
Write-Host "Completed areas     : $($snapshot.completedCapabilityAreas.Count)"
Write-Host "Remaining areas     : $($snapshot.remainingRecommendedAreas.Count)"
Write-Host "Warnings            : $($snapshot.warnings.Count)"

if ($snapshot.completedCapabilityAreas.Count -lt 5) {
    throw "Expected completed capability areas."
}

Write-Host ""
Write-Host "P2 readiness report smoke test completed successfully."
