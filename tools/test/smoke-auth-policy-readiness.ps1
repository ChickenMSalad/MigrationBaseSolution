param(
    [string]$BaseUrl = "http://localhost:5173"
)

$ErrorActionPreference = "Stop"

Write-Host "Auth Policy Readiness Smoke Test"
Write-Host "Base URL: $BaseUrl"
Write-Host ""

$snapshot = Invoke-RestMethod "$BaseUrl/api/cloud/auth/policy-readiness"

Write-Host "Environment          : $($snapshot.environmentName)"
Write-Host "Requires auth        : $($snapshot.requiresAuth)"
Write-Host "Production-like      : $($snapshot.isProductionLike)"
Write-Host "Ready for production : $($snapshot.isReadyForProduction)"
Write-Host "Required policies    : $($snapshot.requiredPolicies.Count)"
Write-Host "Blocking issues      : $($snapshot.blockingIssues.Count)"
Write-Host "Warnings             : $($snapshot.warnings.Count)"

if ($snapshot.requiredPolicies.Count -lt 1) {
    throw "Expected at least one auth policy requirement."
}

Write-Host ""
Write-Host "Auth policy readiness smoke test completed successfully."
