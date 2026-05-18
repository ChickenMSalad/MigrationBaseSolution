param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

Write-Host "Checking operational read route mapping..."
./scripts/admin-api-endpoint-map-smoke-test.ps1 -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Checking operational read API..."
./scripts/operational-mirror-read-api-smoke-test.ps1 -BaseUrl $BaseUrl
