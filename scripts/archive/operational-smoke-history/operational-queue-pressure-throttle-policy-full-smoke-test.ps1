param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

& (Join-Path $scriptRoot "operational-queue-pressure-throttle-policy-route-check.ps1") -BaseUrl $BaseUrl
& (Join-Path $scriptRoot "operational-queue-pressure-throttle-policy-smoke-test.ps1") -BaseUrl $BaseUrl
& (Join-Path $scriptRoot "operational-queue-pressure-throttle-policy-consistency-smoke-test.ps1") -BaseUrl $BaseUrl

Write-Host "Full smoke test passed for P3 Set 154."
