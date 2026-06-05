param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

$url = "$BaseUrl/api/operational/queue-pressure/dashboard"
Write-Host "Calling $url"

$response = Invoke-RestMethod -Method Get -Uri $url

if ($null -eq $response) {
    throw "Dashboard response was null."
}

if ($null -eq $response.dashboard) {
    throw "Expected response.dashboard."
}

if ($null -eq $response.dashboard.generatedAtUtc) {
    throw "Expected response.dashboard.generatedAtUtc."
}

if ($null -eq $response.dashboard.pressureSignals) {
    throw "Expected response.dashboard.pressureSignals."
}

Write-Host "Queue pressure dashboard smoke test passed."
