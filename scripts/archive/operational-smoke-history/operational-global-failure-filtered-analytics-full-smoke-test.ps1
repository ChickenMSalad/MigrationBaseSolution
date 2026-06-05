param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$Limit = 10
)

$ErrorActionPreference = "Stop"

Write-Host "=== Global operational failure filtered analytics full smoke ==="

Write-Host ""
Write-Host "Step 1: Route check"
./scripts/operational-global-failure-filtered-analytics-route-check.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 2: Baseline filtered analytics smoke"
./scripts/operational-global-failure-filtered-analytics-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -Limit $Limit

Write-Host ""
Write-Host "Step 3: Filtered analytics consistency smoke"
./scripts/operational-global-failure-filtered-analytics-consistency-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -Limit $Limit

Write-Host ""
Write-Host "Global operational failure filtered analytics full smoke passed."
