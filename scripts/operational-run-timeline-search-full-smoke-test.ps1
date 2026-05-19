param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Operational run timeline search full smoke ==="

Write-Host ""
Write-Host "Step 1: Route check"
./scripts/operational-run-timeline-search-route-check.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 2: Baseline search smoke"
./scripts/operational-run-timeline-search-smoke-test.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 3: Search consistency smoke"
./scripts/operational-run-timeline-search-consistency-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -SearchText "WorkItem" `
    -Limit 10

Write-Host ""
Write-Host "Step 4: Checkpoint consistency smoke"
./scripts/operational-run-timeline-search-consistency-smoke-test.ps1 `
    -BaseUrl $BaseUrl `
    -SearchText "Checkpoint" `
    -Limit 10

Write-Host ""
Write-Host "Operational run timeline search full smoke passed."
