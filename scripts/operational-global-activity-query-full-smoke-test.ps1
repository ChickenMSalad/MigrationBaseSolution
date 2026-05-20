param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$Limit = 10
)

$ErrorActionPreference = "Stop"

Write-Host "=== Global operational activity query full smoke ==="

Write-Host ""
Write-Host "Step 1: Route check"
./scripts/operational-global-activity-query-route-check.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 2: Baseline query smoke"
./scripts/operational-global-activity-query-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -Limit $Limit

Write-Host ""
Write-Host "Step 3: Query consistency smoke"
./scripts/operational-global-activity-query-consistency-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -Limit $Limit

Write-Host ""
Write-Host "Global operational activity query full smoke passed."
