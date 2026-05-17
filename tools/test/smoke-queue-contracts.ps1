param(
    [string]$BaseUrl = "http://localhost:5173"
)

$ErrorActionPreference = "Stop"

Write-Host "Queue Contract Smoke Test"
Write-Host ""

$provider = Invoke-RestMethod "$BaseUrl/api/cloud/queue/provider"

Write-Host "Provider: $($provider.providerKind)"
Write-Host "Recommended properties: $($provider.recommendedProperties.Count)"

if ($provider.recommendedProperties.Count -lt 4) {
    throw "Expected recommended queue properties."
}

$probe = Invoke-RestMethod -Method Post "$BaseUrl/api/cloud/queue/envelope/probe"

if ([string]::IsNullOrWhiteSpace($probe.idempotencyKey)) {
    throw "Envelope did not contain idempotency key."
}

if ($probe.properties.workspaceId -ne "default") {
    throw "WorkspaceId property mismatch."
}

Write-Host ""
Write-Host "Queue contract smoke test completed successfully."
