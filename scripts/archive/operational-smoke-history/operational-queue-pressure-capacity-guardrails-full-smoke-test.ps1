param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

& (Join-Path $scriptRoot "operational-queue-pressure-capacity-guardrails-route-check.ps1") -BaseUrl $BaseUrl
& (Join-Path $scriptRoot "operational-queue-pressure-capacity-guardrails-smoke-test.ps1") -BaseUrl $BaseUrl
& (Join-Path $scriptRoot "operational-queue-pressure-capacity-guardrails-consistency-smoke-test.ps1") -BaseUrl $BaseUrl

Write-Host "Queue pressure capacity guardrails full smoke passed."
