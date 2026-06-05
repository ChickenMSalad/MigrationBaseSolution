param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Operational run timeline full smoke ==="

Write-Host ""
Write-Host "Step 1: Route check"
./scripts/operational-run-timeline-route-check.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 2: Timeline baseline smoke"
./scripts/operational-run-timeline-smoke-test.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 3: Timeline consistency smoke"
./scripts/operational-run-timeline-consistency-smoke-test.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 4: Timeline ordering smoke"
./scripts/operational-run-timeline-ordering-smoke-test.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Operational run timeline full smoke passed."
