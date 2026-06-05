param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$Limit = 10
)

$ErrorActionPreference = "Stop"

Write-Host "=== Global operational activity feed full smoke ==="

Write-Host ""
Write-Host "Step 1: Route check"
./scripts/operational-global-activity-feed-route-check.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 2: Baseline activity feed smoke"
./scripts/operational-global-activity-feed-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -Limit $Limit

Write-Host ""
Write-Host "Step 3: Activity feed consistency smoke"
./scripts/operational-global-activity-feed-consistency-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -Limit $Limit

Write-Host ""
Write-Host "Global operational activity feed full smoke passed."
