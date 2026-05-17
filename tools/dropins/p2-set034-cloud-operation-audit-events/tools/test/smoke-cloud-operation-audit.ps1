param(
    [string]$BaseUrl = "http://localhost:5173"
)

$ErrorActionPreference = "Stop"

Write-Host "Cloud Operation Audit Smoke Test"
Write-Host "Base URL: $BaseUrl"
Write-Host ""

$names = Invoke-RestMethod "$BaseUrl/api/cloud/audit/operation/event-names"

Write-Host "Category: $($names.category)"
Write-Host "Events  : $($names.eventNames.Count)"

if ($names.eventNames.Count -lt 3) {
    throw "Expected cloud operation audit event names."
}

$probe = Invoke-RestMethod -Method Post "$BaseUrl/api/cloud/audit/operation/probe"

if ($probe.eventCount -lt 1) {
    throw "Expected cloud operation audit events to be written."
}

foreach ($result in $probe.results) {
    if ($result.accepted -ne $true) {
        throw "Cloud operation audit write was not accepted."
    }
}

Write-Host "Audit writes: $($probe.results.Count)"
Write-Host ""
Write-Host "Cloud operation audit smoke test completed successfully."
