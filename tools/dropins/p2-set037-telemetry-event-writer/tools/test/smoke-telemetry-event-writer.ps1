param(
    [string]$BaseUrl = "http://localhost:5173"
)

$ErrorActionPreference = "Stop"

Write-Host "Telemetry Event Writer Smoke Test"
Write-Host "Base URL: $BaseUrl"
Write-Host ""

$probe = Invoke-RestMethod -Method Post "$BaseUrl/api/cloud/telemetry/writer/probe"

Write-Host "Category : $($probe.request.category)"
Write-Host "Event    : $($probe.request.eventName)"
Write-Host "Accepted : $($probe.result.accepted)"
Write-Host "Provider : $($probe.result.providerKind)"
Write-Host "Event id : $($probe.result.eventId)"

if ($probe.result.accepted -ne $true) {
    throw "Telemetry event writer probe was not accepted."
}

Write-Host ""
Write-Host "Telemetry event writer smoke test completed successfully."
