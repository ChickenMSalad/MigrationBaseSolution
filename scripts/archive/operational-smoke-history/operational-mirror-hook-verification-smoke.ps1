param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Before queueing run: last mirror invocation ==="
./scripts/operational-mirror-last-invocation-smoke-test.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Queue one dry run from the Projects UI now."
Write-Host "After the run endpoint returns Accepted, press Enter."
Read-Host

Write-Host ""
Write-Host "=== After queueing run: last mirror invocation ==="
./scripts/operational-mirror-last-invocation-smoke-test.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "=== Mirror write verification ==="
./scripts/operational-mirror-write-verification-smoke-test.ps1 `
    -BaseUrl $BaseUrl
