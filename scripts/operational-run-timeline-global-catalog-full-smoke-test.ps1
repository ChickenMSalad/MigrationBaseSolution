param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Global operational run timeline catalog full smoke ==="

Write-Host ""
Write-Host "Step 1: Route check"
./scripts/operational-run-timeline-global-catalog-route-check.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 2: Baseline global catalog smoke"
./scripts/operational-run-timeline-global-catalog-smoke-test.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 3: Global catalog consistency smoke"
./scripts/operational-run-timeline-global-catalog-consistency-smoke-test.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Global operational run timeline catalog full smoke passed."
