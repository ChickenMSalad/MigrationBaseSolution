param(
    [string]$BaseUrl = "http://localhost:5173"
)

$ErrorActionPreference = "Stop"

Write-Host "Telemetry Sink Smoke Test"
Write-Host "Base URL: $BaseUrl"
Write-Host ""

$provider = Invoke-RestMethod "$BaseUrl/api/cloud/telemetry/provider"

Write-Host "Provider    : $($provider.providerKind)"
Write-Host "Configured  : $($provider.isConfigured)"
Write-Host "Metrics     : $($provider.supportsMetrics)"
Write-Host "Correlation : $($provider.supportsCorrelation)"

if ($provider.isConfigured -ne $true) {
    throw "Telemetry provider is not configured."
}

$probe = Invoke-RestMethod -Method Post "$BaseUrl/api/cloud/telemetry/probe"

if ($probe.result.accepted -ne $true) {
    throw "Telemetry probe was not accepted."
}

Write-Host "Wrote event id: $($probe.result.eventId)"

$recent = Invoke-RestMethod "$BaseUrl/api/cloud/telemetry/recent?take=10"

if ($recent.count -lt 1) {
    throw "Expected at least one recent telemetry event after probe."
}

Write-Host "Recent count: $($recent.count)"
Write-Host ""
Write-Host "Telemetry sink smoke test completed successfully."
