param(
    [string]$BaseUrl = "http://localhost:5173",
    [switch]$AllowUnconfigured
)

$ErrorActionPreference = "Stop"

Write-Host "Azure Queue Dispatch Smoke Test"
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

if ($provider.providerKind -ne "azureStorageQueue") {
    Write-Host "Active provider is not Azure Queue. This is okay for local-only mode."
    exit 0
}

if ($provider.isConfigured -ne $true) {
    if ($AllowUnconfigured) {
        Write-Host "Azure Queue provider is selected but not configured. Skipping dispatch because -AllowUnconfigured was supplied."
        exit 0
    }

    throw "Azure Queue provider is not configured."
}

$probe = Invoke-RestMethod -Method Post "$BaseUrl/api/cloud/queue/dispatch/probe"

if ($probe.result.accepted -ne $true) {
    throw "Azure Queue dispatch probe was not accepted."
}

Write-Host "Azure Queue dispatch accepted. Provider message id: $($probe.result.providerMessageId)"
Write-Host ""
Write-Host "Azure Queue dispatch smoke test completed successfully."
