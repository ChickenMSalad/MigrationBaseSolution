param(
    [string]$BaseUrl = "http://localhost:5173",
    [switch]$AllowUnconfigured
)

$ErrorActionPreference = "Stop"

Write-Host "Queue Receive Smoke Test"
Write-Host "Base URL          : $BaseUrl"
Write-Host "AllowUnconfigured: $AllowUnconfigured"
Write-Host ""

$provider = Invoke-RestMethod "$BaseUrl/api/cloud/queue/receive/provider"

Write-Host "Provider : $($provider.providerKind)"
Write-Host "Queue    : $($provider.logicalQueueName)"
Write-Host "Configured: $($provider.isConfigured)"

if ($provider.warnings.Count -gt 0) {
    Write-Host "Warnings:"
    $provider.warnings | ForEach-Object { Write-Host "  - $_" }
}

if ($provider.isConfigured -ne $true) {
    if ($AllowUnconfigured) {
        Write-Host "Provider is not configured; skipping receive probe because -AllowUnconfigured was supplied."
        exit 0
    }

    throw "Queue receive provider is not configured."
}

$probe = Invoke-RestMethod -Method Post "$BaseUrl/api/cloud/queue/receive/probe"

Write-Host "Received messages: $($probe.messageCount)"
Write-Host ""
Write-Host "Queue receive smoke test completed successfully."
