param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$Limit = 25
)

$ErrorActionPreference = "Stop"

Write-Host "=== Global operational failures full smoke ==="

Write-Host ""
Write-Host "Step 1: Route check"
./scripts/operational-global-failures-route-check.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 2: Baseline failures smoke"
./scripts/operational-global-failures-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -Limit $Limit

Write-Host ""
Write-Host "Step 3: Failures consistency smoke"
./scripts/operational-global-failures-consistency-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -Limit $Limit

Write-Host ""
Write-Host "Global operational failures full smoke passed."
