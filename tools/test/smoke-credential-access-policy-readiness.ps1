param(
    [string]$BaseUrl = "http://localhost:5173"
)

$ErrorActionPreference = "Stop"

Write-Host "Credential Access Policy Readiness Smoke Test"
Write-Host "Base URL: $BaseUrl"
Write-Host ""

$snapshot = Invoke-RestMethod "$BaseUrl/api/cloud/auth/credential-access-policy"

Write-Host "Requires auth               : $($snapshot.requiresAuth)"
Write-Host "Development                 : $($snapshot.isDevelopment)"
Write-Host "Local bypass                : $($snapshot.allowsLocalDevelopmentBypass)"
Write-Host "Dedicated credential scope  : $($snapshot.requiresDedicatedCredentialScope)"
Write-Host "Requires audit              : $($snapshot.requiresAuditForCredentialAccess)"
Write-Host "Requires telemetry          : $($snapshot.requiresTelemetryForCredentialAccess)"
Write-Host "Requirements                : $($snapshot.requirements.Count)"
Write-Host "Blocking issues             : $($snapshot.blockingIssues.Count)"
Write-Host "Warnings                    : $($snapshot.warnings.Count)"

if ($snapshot.requirements.Count -lt 3) {
    throw "Expected credential access policy requirements."
}

if ($snapshot.requiresDedicatedCredentialScope -ne $true) {
    throw "Expected dedicated credential scope to be required."
}

Write-Host ""
Write-Host "Credential access policy readiness smoke test completed successfully."
