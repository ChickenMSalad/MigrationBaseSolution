param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "=== MIRROR STATUS ==="
./scripts/operational-mirror-status-smoke-test.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "=== MIRROR READINESS ==="
./scripts/operational-mirror-readiness-smoke-test.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "=== MIRROR ENABLEMENT GUARD ==="
./scripts/operational-mirror-enablement-guard-smoke-test.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "=== SQL SCHEMA ==="
./scripts/operational-sql-schema-smoke-test.ps1 `
    -BaseUrl $BaseUrl
