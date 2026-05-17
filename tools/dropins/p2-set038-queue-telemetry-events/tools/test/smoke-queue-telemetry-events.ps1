param(
    [string]$BaseUrl = "http://localhost:5173"
)

$ErrorActionPreference = "Stop"

Write-Host "Queue Telemetry Events Smoke Test"
Write-Host "Base URL: $BaseUrl"
Write-Host ""

$names = Invoke-RestMethod "$BaseUrl/api/cloud/queue/telemetry/event-names"

Write-Host "Category: $($names.category)"
Write-Host "Events  : $($names.eventNames.Count)"

if ($names.eventNames.Count -lt 3) {
    throw "Expected queue telemetry event names."
}

$probe = Invoke-RestMethod -Method Post "$BaseUrl/api/cloud/queue/telemetry/probe"

if ($probe.telemetryResults.Count -lt 2) {
    throw "Expected at least two telemetry write results."
}

foreach ($result in $probe.telemetryResults) {
    if ($result.accepted -ne $true) {
        throw "Queue telemetry event write was not accepted."
    }
}

Write-Host "Telemetry writes: $($probe.telemetryResults.Count)"
Write-Host ""
Write-Host "Queue telemetry events smoke test completed successfully."
