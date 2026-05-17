param(
    [string]$BaseUrl = "http://localhost:5173",
    [switch]$AllowUnconfigured
)

$ErrorActionPreference = "Stop"

Write-Host "Queue Dispatch Smoke Test"
Write-Host "Base URL          : $BaseUrl"
Write-Host "AllowUnconfigured: $AllowUnconfigured"
Write-Host ""

$provider = Invoke-RestMethod "$BaseUrl/api/cloud/queue/dispatch/provider"

Write-Host "Provider : $($provider.providerKind)"
Write-Host "Queue    : $($provider.logicalQueueName)"
Write-Host "Configured: $($provider.isConfigured)"

if ($provider.warnings.Count -gt 0) {
    Write-Host "Warnings:"
    $provider.warnings | ForEach-Object { Write-Host "  - $_" }
}

if ($provider.isConfigured -ne $true) {
    if ($AllowUnconfigured) {
        Write-Host "Provider is not configured; skipping dispatch probe because -AllowUnconfigured was supplied."
        exit 0
    }

    throw "Queue dispatch provider is not configured."
}

$probe = Invoke-RestMethod -Method Post "$BaseUrl/api/cloud/queue/dispatch/probe"

if ($probe.result.accepted -ne $true) {
    throw "Dispatch probe was not accepted."
}

if ([string]::IsNullOrWhiteSpace($probe.result.idempotencyKey)) {
    throw "Dispatch probe did not return idempotency key."
}

Write-Host "Dispatch accepted with message id $($probe.result.messageId)"
Write-Host ""
Write-Host "Queue dispatch smoke test completed successfully."
