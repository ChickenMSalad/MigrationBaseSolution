param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Operational dashboard full smoke ==="

Write-Host ""
Write-Host "Step 1: Route check"
./scripts/operational-dashboard-route-check.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 2: Run dashboard consistency"
./scripts/operational-run-dashboard-consistency-smoke-test.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 3: Dispatcher dashboard consistency"
./scripts/operational-dispatcher-dashboard-consistency-smoke-test.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Operational dashboard full smoke passed."
