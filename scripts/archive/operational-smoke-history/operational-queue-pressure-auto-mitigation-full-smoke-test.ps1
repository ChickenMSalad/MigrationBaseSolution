param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

& (Join-Path $scriptRoot "operational-queue-pressure-auto-mitigation-route-check.ps1") -BaseUrl $BaseUrl
& (Join-Path $scriptRoot "operational-queue-pressure-auto-mitigation-smoke-test.ps1") -BaseUrl $BaseUrl
& (Join-Path $scriptRoot "operational-queue-pressure-auto-mitigation-consistency-smoke-test.ps1") -BaseUrl $BaseUrl

Write-Host "Full smoke test passed for P3 Set 155."
