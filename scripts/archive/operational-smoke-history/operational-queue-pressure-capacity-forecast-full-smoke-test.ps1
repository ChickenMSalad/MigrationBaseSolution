param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

& (Join-Path $scriptRoot "operational-queue-pressure-capacity-forecast-route-check.ps1") -BaseUrl $BaseUrl
& (Join-Path $scriptRoot "operational-queue-pressure-capacity-forecast-smoke-test.ps1") -BaseUrl $BaseUrl
& (Join-Path $scriptRoot "operational-queue-pressure-capacity-forecast-consistency-smoke-test.ps1") -BaseUrl $BaseUrl

Write-Host "Queue pressure capacity forecast full smoke passed."
