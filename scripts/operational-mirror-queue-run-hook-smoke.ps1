param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

Write-Host "=== QueueRun mirror hook smoke ==="
Write-Host ""

Write-Host "Step 1: Current mirror guard"
./scripts/operational-mirror-enablement-guard-smoke-test.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 2: Last mirror invocation before QueueRun"
./scripts/operational-mirror-last-invocation-smoke-test.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Queue one DRY RUN from the Projects UI using the Queue Run button."
Write-Host "After the endpoint returns Accepted, press Enter."
Read-Host

Write-Host ""
Write-Host "Step 3: Last mirror invocation after QueueRun"
./scripts/operational-mirror-last-invocation-smoke-test.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 4: Mirror write verification"
./scripts/operational-mirror-write-verification-smoke-test.ps1 `
    -BaseUrl $BaseUrl
