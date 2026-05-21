param([string]$BaseUrl = "https://localhost:55436")
$ErrorActionPreference = "Stop"
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
& (Join-Path $scriptRoot "operational-queue-pressure-readout-route-check.ps1") -BaseUrl $BaseUrl
& (Join-Path $scriptRoot "operational-queue-pressure-readout-smoke-test.ps1") -BaseUrl $BaseUrl
& (Join-Path $scriptRoot "operational-queue-pressure-readout-consistency-smoke-test.ps1") -BaseUrl $BaseUrl
Write-Host "Full smoke passed for operational queue pressure readout."
