param(
    [string]$BaseUrl = "http://localhost:5173"
)

$ErrorActionPreference = "Stop"

Write-Host "Operational Mode Smoke Test"
Write-Host "Base URL: $BaseUrl"
Write-Host ""

$snapshot = Invoke-RestMethod "$BaseUrl/api/cloud/operations/mode"

Write-Host "Environment       : $($snapshot.environmentName)"
Write-Host "Mode              : $($snapshot.mode)"
Write-Host "Diagnostics only  : $($snapshot.isDiagnosticsOnly)"
Write-Host "Production ready  : $($snapshot.isProductionReady)"
Write-Host "Live queue allowed: $($snapshot.isLiveQueueExecutionAllowed)"
Write-Host "Capabilities      : $($snapshot.capabilities.Count)"
Write-Host "Disabled          : $($snapshot.disabledCapabilities.Count)"

if ([string]::IsNullOrWhiteSpace($snapshot.mode)) {
    throw "Expected operational mode."
}

if ($snapshot.capabilities.Count -lt 1) {
    throw "Expected capabilities."
}

Write-Host ""
Write-Host "Operational mode smoke test completed successfully."
