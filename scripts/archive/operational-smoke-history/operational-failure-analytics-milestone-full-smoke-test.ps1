param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$Limit = 10,
    [int]$SampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "=== Operational Failure Analytics Milestone Full Smoke ==="

Write-Host ""
Write-Host "Step 1: Failure analytics API surface audit"
./scripts/operational-failure-analytics-api-surface-audit.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 2: Failure analytics health snapshot"
./scripts/operational-failure-analytics-health-snapshot-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -Limit $Limit `
    -SampleLimit $SampleLimit

Write-Host ""
Write-Host "Step 3: Failure analytics dashboard consistency"
./scripts/operational-global-failure-analytics-dashboard-full-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -RecentLimit $Limit `
    -MetricsSampleLimit $SampleLimit

Write-Host ""
Write-Host "Step 4: Filtered analytics consistency"
./scripts/operational-global-failure-filtered-analytics-full-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -Limit $Limit

Write-Host ""
Write-Host "Step 5: Preset dashboard consistency"
./scripts/operational-global-failure-analytics-preset-dashboard-full-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -PresetKey "all-recent" `
    -Limit $Limit

Write-Host ""
Write-Host "Step 6: Favorite preset consistency"
./scripts/operational-global-failure-analytics-preset-favorites-full-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -Limit $Limit

Write-Host ""
Write-Host "Operational failure analytics milestone full smoke passed."
