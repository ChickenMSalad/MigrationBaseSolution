param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$ActivityRecentLimit = 10,
    [int]$MetricsSampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "=== Global operational run health dashboard full smoke ==="

Write-Host ""
Write-Host "Step 1: Route check"
./scripts/operational-global-run-health-dashboard-route-check.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 2: Baseline dashboard smoke"
./scripts/operational-global-run-health-dashboard-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -ActivityRecentLimit $ActivityRecentLimit `
    -MetricsSampleLimit $MetricsSampleLimit

Write-Host ""
Write-Host "Step 3: Dashboard consistency smoke"
./scripts/operational-global-run-health-dashboard-consistency-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -ActivityRecentLimit $ActivityRecentLimit `
    -MetricsSampleLimit $MetricsSampleLimit

Write-Host ""
Write-Host "Global operational run health dashboard full smoke passed."
