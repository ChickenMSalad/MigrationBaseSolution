param(
    [string]$BaseUrl = "http://localhost:5173"
)

$ErrorActionPreference = "Stop"

Write-Host "Queue Poison Handling Smoke Test"
Write-Host "Base URL: $BaseUrl"
Write-Host ""

$plan = Invoke-RestMethod "$BaseUrl/api/cloud/queue/poison-handling"

Write-Host "Provider      : $($plan.providerKind)"
Write-Host "Queue         : $($plan.logicalQueueName)"
Write-Host "Max attempts  : $($plan.maxAttempts)"
Write-Host "Strategy      : $($plan.poisonStrategy)"
Write-Host "Failure kind  : $($plan.failureArtifactKind)"

if ($plan.maxAttempts -lt 1) {
    throw "MaxAttempts should be at least 1."
}

$recommendation = Invoke-RestMethod "$BaseUrl/api/cloud/queue/poison-handling/recommendation"

if ([string]::IsNullOrWhiteSpace($recommendation.recommendation)) {
    throw "Expected recommendation text."
}

Write-Host "Recommendation: $($recommendation.recommendation)"
Write-Host ""
Write-Host "Queue poison handling smoke test completed successfully."
