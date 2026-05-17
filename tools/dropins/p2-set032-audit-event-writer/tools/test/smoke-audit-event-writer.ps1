param(
    [string]$BaseUrl = "http://localhost:5173"
)

$ErrorActionPreference = "Stop"

Write-Host "Audit Event Writer Smoke Test"
Write-Host "Base URL: $BaseUrl"
Write-Host ""

$probe = Invoke-RestMethod -Method Post "$BaseUrl/api/cloud/audit/writer/probe"

Write-Host "Category : $($probe.request.category)"
Write-Host "Event    : $($probe.request.eventName)"
Write-Host "Accepted : $($probe.result.accepted)"
Write-Host "Provider : $($probe.result.providerKind)"
Write-Host "Audit id : $($probe.result.auditId)"

if ($probe.result.accepted -ne $true) {
    throw "Audit event writer probe was not accepted."
}

Write-Host ""
Write-Host "Audit event writer smoke test completed successfully."
