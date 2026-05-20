param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

Write-Host "=== P3 Milestone 100 Full Smoke ==="

Write-Host ""
Write-Host "Step 1: API surface audit"
./scripts/operational-api-surface-audit.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 2: Health snapshot"
./scripts/operational-p3-health-snapshot-smoke-test.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 3: Dashboard consistency"
./scripts/operational-global-activity-dashboard-full-smoke-test.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 4: Timeline catalog consistency"
./scripts/operational-run-timeline-global-catalog-full-smoke-test.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 5: Recent failures consistency"
./scripts/operational-global-failures-full-smoke-test.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "P3 milestone 100 full smoke passed."
