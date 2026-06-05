param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$ActivityRecentLimit = 10,
    [int]$MetricsSampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "=== Global operational run health operations center full smoke ==="

Write-Host ""
Write-Host "Step 1: Route check"
./scripts/operational-global-run-health-operations-center-route-check.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 2: Baseline operations center smoke"
./scripts/operational-global-run-health-operations-center-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -ActivityRecentLimit $ActivityRecentLimit `
    -MetricsSampleLimit $MetricsSampleLimit

Write-Host ""
Write-Host "Step 3: Operations center consistency smoke"
./scripts/operational-global-run-health-operations-center-consistency-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -ActivityRecentLimit $ActivityRecentLimit `
    -MetricsSampleLimit $MetricsSampleLimit

Write-Host ""
Write-Host "Global operational run health operations center full smoke passed."
