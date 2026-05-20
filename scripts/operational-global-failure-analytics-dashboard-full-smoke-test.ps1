param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$RecentLimit = 10,
    [int]$MetricsSampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "=== Global operational failure analytics dashboard full smoke ==="

Write-Host ""
Write-Host "Step 1: Route check"
./scripts/operational-global-failure-analytics-dashboard-route-check.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 2: Baseline analytics dashboard smoke"
./scripts/operational-global-failure-analytics-dashboard-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -RecentLimit $RecentLimit `
    -MetricsSampleLimit $MetricsSampleLimit

Write-Host ""
Write-Host "Step 3: Analytics dashboard consistency smoke"
./scripts/operational-global-failure-analytics-dashboard-consistency-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -RecentLimit $RecentLimit `
    -MetricsSampleLimit $MetricsSampleLimit

Write-Host ""
Write-Host "Global operational failure analytics dashboard full smoke passed."
