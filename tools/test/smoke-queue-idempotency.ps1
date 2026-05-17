param(
    [string]$BaseUrl = "http://localhost:5173"
)

$ErrorActionPreference = "Stop"

Write-Host "Queue Idempotency Smoke Test"
Write-Host "Base URL: $BaseUrl"
Write-Host ""

$plan = Invoke-RestMethod "$BaseUrl/api/cloud/queue/idempotency?projectId=sample-project&runId=sample-run&messageType=migration.run.execute"

if ([string]::IsNullOrWhiteSpace($plan.idempotencyKey)) {
    throw "Idempotency key was empty."
}

if ([string]::IsNullOrWhiteSpace($plan.hashedIdempotencyKey)) {
    throw "Hashed idempotency key was empty."
}

if ([string]::IsNullOrWhiteSpace($plan.leaseResource)) {
    throw "Lease resource was empty."
}

Write-Host "Idempotency key: $($plan.idempotencyKey)"
Write-Host "Lease resource : $($plan.leaseResource)"

$serialization = Invoke-RestMethod -Method Post "$BaseUrl/api/cloud/queue/envelope/serialize"

if ($serialization.roundTripMatches -ne $true) {
    throw "Queue envelope serialization did not round-trip."
}

Write-Host "Serialization round-trip succeeded."
Write-Host ""
Write-Host "Queue idempotency smoke test completed successfully."
