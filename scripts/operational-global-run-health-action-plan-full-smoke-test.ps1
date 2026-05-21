param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$RecentLimit = 10,
    [int]$MetricsSampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "=== Global operational run health action plan full smoke ==="

Write-Host ""
Write-Host "Step 1: Route check"
./scripts/operational-global-run-health-action-plan-route-check.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 2: Baseline action plan smoke"
./scripts/operational-global-run-health-action-plan-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -RecentLimit $RecentLimit `
    -MetricsSampleLimit $MetricsSampleLimit

Write-Host ""
Write-Host "Step 3: Action plan consistency smoke"
./scripts/operational-global-run-health-action-plan-consistency-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -RecentLimit $RecentLimit `
    -MetricsSampleLimit $MetricsSampleLimit

Write-Host ""
Write-Host "Global operational run health action plan full smoke passed."
