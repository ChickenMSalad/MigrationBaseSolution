param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$MetricsSampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "=== Operational dispatcher pressure full smoke ==="

./scripts/operational-dispatcher-pressure-route-check.ps1 `
    -BaseUrl $BaseUrl

./scripts/operational-dispatcher-pressure-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -MetricsSampleLimit $MetricsSampleLimit

./scripts/operational-dispatcher-pressure-consistency-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -MetricsSampleLimit $MetricsSampleLimit

Write-Host "Operational dispatcher pressure full smoke passed."
