param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

Write-Host "Checking operational mirror enablement guard..."
./scripts/operational-mirror-enablement-guard-smoke-test.ps1 `
    -BaseUrl $BaseUrl
