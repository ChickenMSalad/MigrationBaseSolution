param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

$url = "$BaseUrl/api/operational/queue-pressure/dashboard"
Write-Host "Checking dashboard consistency at $url"

$response = Invoke-RestMethod -Method Get -Uri $url

if ($null -eq $response.dashboard.readiness) {
    throw "Expected response.dashboard.readiness."
}

if ($null -eq $response.dashboard.pressureSignals.queueDepth) {
    throw "Expected response.dashboard.pressureSignals.queueDepth."
}

if ($null -eq $response.dashboard.pressureSignals.dispatcherPressure) {
    throw "Expected response.dashboard.pressureSignals.dispatcherPressure."
}

Write-Host "Queue pressure dashboard consistency smoke test passed."
