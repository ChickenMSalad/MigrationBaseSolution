param(
    [string]$BaseUrl = "http://localhost:5173"
)

$ErrorActionPreference = "Stop"

Write-Host "Audit Persistence Smoke Test"
Write-Host "Base URL: $BaseUrl"
Write-Host ""

$provider = Invoke-RestMethod "$BaseUrl/api/cloud/audit/persistence/provider"

Write-Host "Provider : $($provider.providerKind)"
Write-Host "Durable  : $($provider.isDurable)"
Write-Host "Query    : $($provider.supportsQuery)"

if ($provider.isConfigured -ne $true) {
    throw "Audit persistence provider is not configured."
}

$probe = Invoke-RestMethod -Method Post "$BaseUrl/api/cloud/audit/persistence/probe"

if ($probe.result.accepted -ne $true) {
    throw "Audit persistence probe was not accepted."
}

Write-Host "Wrote audit id: $($probe.result.auditId)"

$recent = Invoke-RestMethod "$BaseUrl/api/cloud/audit/persistence/recent?take=10"

if ($recent.count -lt 1) {
    throw "Expected at least one recent audit record after probe."
}

Write-Host "Recent count: $($recent.count)"
Write-Host ""
Write-Host "Audit persistence smoke test completed successfully."
