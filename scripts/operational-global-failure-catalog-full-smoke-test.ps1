param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$SampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "=== Global operational failure catalog full smoke ==="

Write-Host ""
Write-Host "Step 1: Route check"
./scripts/operational-global-failure-catalog-route-check.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 2: Baseline catalog smoke"
./scripts/operational-global-failure-catalog-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -SampleLimit $SampleLimit

Write-Host ""
Write-Host "Step 3: Catalog consistency smoke"
./scripts/operational-global-failure-catalog-consistency-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -SampleLimit $SampleLimit

Write-Host ""
Write-Host "Global operational failure catalog full smoke passed."
