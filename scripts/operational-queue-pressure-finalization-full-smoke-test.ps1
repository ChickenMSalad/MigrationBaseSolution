param([string]$BaseUrl = "https://localhost:55436")
$ErrorActionPreference = "Stop"
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
& (Join-Path $scriptRoot "operational-queue-pressure-finalization-route-check.ps1") -BaseUrl $BaseUrl
& (Join-Path $scriptRoot "operational-queue-pressure-finalization-smoke-test.ps1") -BaseUrl $BaseUrl
& (Join-Path $scriptRoot "operational-queue-pressure-finalization-consistency-smoke-test.ps1") -BaseUrl $BaseUrl
Write-Host "Full smoke passed for operational queue pressure finalization."
