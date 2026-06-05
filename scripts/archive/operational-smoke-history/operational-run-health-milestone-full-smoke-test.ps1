param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$Limit = 10,
    [int]$MetricsSampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "=== Operational Run Health Milestone Full Smoke ==="

Write-Host ""
Write-Host "Step 1: API surface audit"
./scripts/operational-run-health-api-surface-audit.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 2: Contract shape smoke"
./scripts/operational-run-health-contract-shape-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -Limit $Limit `
    -MetricsSampleLimit $MetricsSampleLimit

Write-Host ""
Write-Host "Step 3: Health summary full smoke"
./scripts/operational-global-run-health-summary-full-smoke-test.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 4: Health dashboard full smoke"
./scripts/operational-global-run-health-dashboard-full-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -ActivityRecentLimit $Limit `
    -MetricsSampleLimit $MetricsSampleLimit

Write-Host ""
Write-Host "Step 5: Snapshot full smoke"
./scripts/operational-global-run-health-snapshot-full-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -RecentLimit $Limit `
    -MetricsSampleLimit $MetricsSampleLimit

Write-Host ""
Write-Host "Step 6: Trend summary full smoke"
./scripts/operational-global-run-health-trend-summary-full-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -RecentLimit $Limit `
    -MetricsSampleLimit $MetricsSampleLimit

Write-Host ""
Write-Host "Step 7: Detailed risk full smoke"
./scripts/operational-global-run-health-detailed-risk-full-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -RecentLimit $Limit `
    -MetricsSampleLimit $MetricsSampleLimit

Write-Host ""
Write-Host "Step 8: Recommendations full smoke"
./scripts/operational-global-run-health-recommendations-full-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -RecentLimit $Limit `
    -MetricsSampleLimit $MetricsSampleLimit

Write-Host ""
Write-Host "Step 9: Action plan full smoke"
./scripts/operational-global-run-health-action-plan-full-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -RecentLimit $Limit `
    -MetricsSampleLimit $MetricsSampleLimit

Write-Host ""
Write-Host "Step 10: Operations center full smoke"
./scripts/operational-global-run-health-operations-center-full-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -ActivityRecentLimit $Limit `
    -MetricsSampleLimit $MetricsSampleLimit

Write-Host ""
Write-Host "Operational run health milestone full smoke passed."
