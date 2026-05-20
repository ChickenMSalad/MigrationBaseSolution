param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Global operational run health summary full smoke ==="

Write-Host ""
Write-Host "Step 1: Route check"
./scripts/operational-global-run-health-summary-route-check.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 2: Baseline run health smoke"
./scripts/operational-global-run-health-summary-smoke-test.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 3: Run health consistency smoke"
./scripts/operational-global-run-health-summary-consistency-smoke-test.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Global operational run health summary full smoke passed."
