param(
    [string]$BaseUrl = "http://localhost:5173"
)

$ErrorActionPreference = "Stop"

Write-Host "Cloud Operation Telemetry Smoke Test"
Write-Host "Base URL: $BaseUrl"
Write-Host ""

$names = Invoke-RestMethod "$BaseUrl/api/cloud/telemetry/operation/event-names"

Write-Host "Category: $($names.category)"
Write-Host "Events  : $($names.eventNames.Count)"

if ($names.eventNames.Count -lt 3) {
    throw "Expected cloud operation telemetry event names."
}

$probe = Invoke-RestMethod -Method Post "$BaseUrl/api/cloud/telemetry/operation/probe"

if ($probe.eventCount -lt 1) {
    throw "Expected cloud operation telemetry events to be written."
}

foreach ($result in $probe.results) {
    if ($result.accepted -ne $true) {
        throw "Cloud operation telemetry write was not accepted."
    }
}

Write-Host "Telemetry writes: $($probe.results.Count)"
Write-Host ""
Write-Host "Cloud operation telemetry smoke test completed successfully."
