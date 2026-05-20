param(
    [string]$BaseUrl = "https://localhost:55436",
    [string]$PresetKey = "all-recent",
    [int]$Limit = 10
)

$ErrorActionPreference = "Stop"

Write-Host "=== Global operational failure analytics preset dashboard full smoke ==="

Write-Host ""
Write-Host "Step 1: Route check"
./scripts/operational-global-failure-analytics-preset-dashboard-route-check.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 2: Baseline preset dashboard smoke"
./scripts/operational-global-failure-analytics-preset-dashboard-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -PresetKey $PresetKey `
    -Limit $Limit

Write-Host ""
Write-Host "Step 3: Preset dashboard consistency smoke"
./scripts/operational-global-failure-analytics-preset-dashboard-consistency-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -PresetKey $PresetKey `
    -Limit $Limit

Write-Host ""
Write-Host "Global operational failure analytics preset dashboard full smoke passed."
