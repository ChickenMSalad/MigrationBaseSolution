param([string]$BaseUrl = "https://localhost:55436")
$ErrorActionPreference = "Stop"

Write-Host "=== Global operational queue depth full smoke ==="

Write-Host ""
Write-Host "Step 1: Route check"
./scripts/operational-global-queue-depth-route-check.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 2: Baseline queue depth smoke"
./scripts/operational-global-queue-depth-smoke-test.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 3: Queue depth consistency smoke"
./scripts/operational-global-queue-depth-consistency-smoke-test.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Global operational queue depth full smoke passed."
