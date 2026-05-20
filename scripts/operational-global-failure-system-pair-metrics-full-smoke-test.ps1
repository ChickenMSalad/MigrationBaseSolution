param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$SampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "=== Global operational failure system-pair metrics full smoke ==="

Write-Host ""
Write-Host "Step 1: Route check"
./scripts/operational-global-failure-system-pair-metrics-route-check.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 2: Baseline system-pair metrics smoke"
./scripts/operational-global-failure-system-pair-metrics-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -SampleLimit $SampleLimit

Write-Host ""
Write-Host "Step 3: System-pair metrics consistency smoke"
./scripts/operational-global-failure-system-pair-metrics-consistency-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -SampleLimit $SampleLimit

Write-Host ""
Write-Host "Global operational failure system-pair metrics full smoke passed."
