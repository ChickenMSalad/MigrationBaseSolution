param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$SampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "=== Global operational activity metrics full smoke ==="

Write-Host ""
Write-Host "Step 1: Route check"
./scripts/operational-global-activity-metrics-route-check.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 2: Baseline metrics smoke"
./scripts/operational-global-activity-metrics-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -SampleLimit $SampleLimit

Write-Host ""
Write-Host "Step 3: Metrics consistency smoke"
./scripts/operational-global-activity-metrics-consistency-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -SampleLimit $SampleLimit

Write-Host ""
Write-Host "Global operational activity metrics full smoke passed."
