param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

& (Join-Path $scriptRoot "operational-queue-pressure-escalation-guide-route-check.ps1") -BaseUrl $BaseUrl
& (Join-Path $scriptRoot "operational-queue-pressure-escalation-guide-smoke-test.ps1") -BaseUrl $BaseUrl
& (Join-Path $scriptRoot "operational-queue-pressure-escalation-guide-consistency-smoke-test.ps1") -BaseUrl $BaseUrl

Write-Host "Queue pressure escalation-guide full smoke test passed."
