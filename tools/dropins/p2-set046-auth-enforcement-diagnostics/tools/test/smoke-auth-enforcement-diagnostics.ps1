param(
    [string]$BaseUrl = "http://localhost:5173"
)

$ErrorActionPreference = "Stop"

Write-Host "Auth Enforcement Diagnostics Smoke Test"
Write-Host "Base URL: $BaseUrl"
Write-Host ""

$snapshot = Invoke-RestMethod "$BaseUrl/api/cloud/auth/enforcement-diagnostics"

Write-Host "Global auth required : $($snapshot.globalAuthRequired)"
Write-Host "Production mode      : $($snapshot.productionModeEnabled)"
Write-Host "Diagnostics          : $($snapshot.diagnostics.Count)"
Write-Host "Warnings             : $($snapshot.warnings.Count)"

if ($snapshot.diagnostics.Count -lt 3) {
    throw "Expected auth enforcement diagnostics."
}

Write-Host ""
Write-Host "Auth enforcement diagnostics smoke test completed successfully."
