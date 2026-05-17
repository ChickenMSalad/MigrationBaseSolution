param(
    [string]$BaseUrl = "http://localhost:5173"
)

$ErrorActionPreference = "Stop"

Write-Host "Endpoint Policy Inventory Smoke Test"
Write-Host "Base URL: $BaseUrl"
Write-Host ""

$snapshot = Invoke-RestMethod "$BaseUrl/api/cloud/auth/endpoint-policy-inventory"

Write-Host "Items                    : $($snapshot.items.Count)"
Write-Host "Read-only count          : $($snapshot.readOnlyCount)"
Write-Host "Mutating count           : $($snapshot.mutatingCount)"
Write-Host "Credential-sensitive     : $($snapshot.credentialSensitiveCount)"
Write-Host "Operationally-sensitive  : $($snapshot.operationallySensitiveCount)"

if ($snapshot.items.Count -lt 5) {
    throw "Expected endpoint policy inventory items."
}

if ($snapshot.credentialSensitiveCount -lt 1) {
    throw "Expected at least one credential-sensitive route area."
}

Write-Host ""
Write-Host "Endpoint policy inventory smoke test completed successfully."
