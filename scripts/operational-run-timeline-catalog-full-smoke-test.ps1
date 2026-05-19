param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Operational run timeline catalog full smoke ==="

Write-Host ""
Write-Host "Step 1: Route check"
./scripts/operational-run-timeline-catalog-route-check.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 2: Baseline catalog smoke"
./scripts/operational-run-timeline-event-type-catalog-smoke-test.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 3: Catalog consistency smoke"
./scripts/operational-run-timeline-catalog-consistency-smoke-test.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Operational run timeline catalog full smoke passed."
