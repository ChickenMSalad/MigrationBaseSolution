param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Operational mirror after schema repair smoke ==="
Write-Host ""

Write-Host "Step 1: SQL schema smoke test"
./scripts/operational-sql-schema-smoke-test.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 2: Enablement guard"
./scripts/operational-mirror-enablement-guard-smoke-test.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 3: Last invocation before QueueRun"
./scripts/operational-mirror-last-invocation-smoke-test.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Queue one DRY RUN from the Projects UI now."
Write-Host "After the run endpoint returns Accepted, press Enter."
Read-Host

Write-Host ""
Write-Host "Step 4: Last invocation after QueueRun"
./scripts/operational-mirror-last-invocation-smoke-test.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 5: Write verification"
./scripts/operational-mirror-write-verification-smoke-test.ps1 `
    -BaseUrl $BaseUrl
