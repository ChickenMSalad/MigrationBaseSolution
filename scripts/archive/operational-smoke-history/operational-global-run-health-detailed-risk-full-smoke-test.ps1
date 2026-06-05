param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$RecentLimit = 10,
    [int]$MetricsSampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "=== Global operational run health detailed risk full smoke ==="

Write-Host ""
Write-Host "Step 1: Route check"
./scripts/operational-global-run-health-detailed-risk-route-check.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 2: Baseline detailed risk smoke"
./scripts/operational-global-run-health-detailed-risk-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -RecentLimit $RecentLimit `
    -MetricsSampleLimit $MetricsSampleLimit

Write-Host ""
Write-Host "Step 3: Detailed risk consistency smoke"
./scripts/operational-global-run-health-detailed-risk-consistency-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -RecentLimit $RecentLimit `
    -MetricsSampleLimit $MetricsSampleLimit

Write-Host ""
Write-Host "Global operational run health detailed risk full smoke passed."
