param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$RecentLimit = 10,
    [int]$MetricsSampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "=== Global operational run health trend summary full smoke ==="

Write-Host ""
Write-Host "Step 1: Route check"
./scripts/operational-global-run-health-trend-summary-route-check.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 2: Baseline trend summary smoke"
./scripts/operational-global-run-health-trend-summary-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -RecentLimit $RecentLimit `
    -MetricsSampleLimit $MetricsSampleLimit

Write-Host ""
Write-Host "Step 3: Trend summary consistency smoke"
./scripts/operational-global-run-health-trend-summary-consistency-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -RecentLimit $RecentLimit `
    -MetricsSampleLimit $MetricsSampleLimit

Write-Host ""
Write-Host "Global operational run health trend summary full smoke passed."
