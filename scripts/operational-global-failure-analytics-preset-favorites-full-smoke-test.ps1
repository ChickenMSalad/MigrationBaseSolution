param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$Limit = 10
)

$ErrorActionPreference = "Stop"

Write-Host "=== Global operational failure analytics preset favorites full smoke ==="

Write-Host ""
Write-Host "Step 1: Route check"
./scripts/operational-global-failure-analytics-preset-favorites-route-check.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 2: Baseline favorites smoke"
./scripts/operational-global-failure-analytics-preset-favorites-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -FavoriteKey "triage" `
    -Limit $Limit

Write-Host ""
Write-Host "Step 3: Favorites consistency smoke"
./scripts/operational-global-failure-analytics-preset-favorites-consistency-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -Limit $Limit

Write-Host ""
Write-Host "Global operational failure analytics preset favorites full smoke passed."
