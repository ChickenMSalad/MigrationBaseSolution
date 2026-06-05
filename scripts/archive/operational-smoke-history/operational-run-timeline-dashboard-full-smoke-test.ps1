param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$PreviewLimit = 5
)

$ErrorActionPreference = "Stop"

Write-Host "=== Operational run timeline dashboard full smoke ==="

Write-Host ""
Write-Host "Step 1: Route check"
./scripts/operational-run-timeline-dashboard-route-check.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 2: Baseline dashboard smoke"
./scripts/operational-run-timeline-dashboard-smoke-test.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 3: Dashboard consistency smoke"
./scripts/operational-run-timeline-dashboard-consistency-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -PreviewLimit $PreviewLimit

Write-Host ""
Write-Host "Operational run timeline dashboard full smoke passed."
