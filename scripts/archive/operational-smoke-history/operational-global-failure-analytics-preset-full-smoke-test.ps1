param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$Limit = 10
)

$ErrorActionPreference = "Stop"

Write-Host "=== Global operational failure analytics preset full smoke ==="

Write-Host ""
Write-Host "Step 1: Route check"
./scripts/operational-global-failure-analytics-preset-route-check.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 2: Baseline preset smoke"
./scripts/operational-global-failure-filtered-analytics-query-preset-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -Limit $Limit

Write-Host ""
Write-Host "Step 3: Preset consistency smoke"
./scripts/operational-global-failure-analytics-preset-consistency-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -Limit $Limit

Write-Host ""
Write-Host "Global operational failure analytics preset full smoke passed."
