param(
    [string]$BaseUrl = "http://localhost:5173"
)

$ErrorActionPreference = "Stop"

Write-Host "Queue Audit Events Smoke Test"
Write-Host "Base URL: $BaseUrl"
Write-Host ""

$names = Invoke-RestMethod "$BaseUrl/api/cloud/queue/audit/event-names"

Write-Host "Category: $($names.category)"
Write-Host "Events  : $($names.eventNames.Count)"

if ($names.eventNames.Count -lt 3) {
    throw "Expected queue audit event names."
}

$probe = Invoke-RestMethod -Method Post "$BaseUrl/api/cloud/queue/audit/probe"

if ($probe.auditResults.Count -lt 2) {
    throw "Expected at least two audit write results."
}

foreach ($result in $probe.auditResults) {
    if ($result.accepted -ne $true) {
        throw "Queue audit event write was not accepted."
    }
}

Write-Host "Audit writes: $($probe.auditResults.Count)"
Write-Host ""
Write-Host "Queue audit events smoke test completed successfully."
