param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$SampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "=== Global operational failure run-status metrics full smoke ==="

Write-Host ""
Write-Host "Step 1: Route check"
./scripts/operational-global-failure-run-status-metrics-route-check.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 2: Baseline run-status metrics smoke"
./scripts/operational-global-failure-run-status-metrics-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -SampleLimit $SampleLimit

Write-Host ""
Write-Host "Step 3: Run-status metrics consistency smoke"
./scripts/operational-global-failure-run-status-metrics-consistency-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -SampleLimit $SampleLimit

Write-Host ""
Write-Host "Global operational failure run-status metrics full smoke passed."
