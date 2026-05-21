param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

$endpointsUrl = "$BaseUrl/api/system/endpoints"
Write-Host "Checking endpoint discovery at $endpointsUrl"

$response = Invoke-RestMethod -Method Get -Uri $endpointsUrl

$json = $response | ConvertTo-Json -Depth 50

if ($json -notmatch "/api/operational/queue-pressure/dashboard") {
    throw "Missing expected route: /api/operational/queue-pressure/dashboard"
}

if ($json -match "/api/api/operational/queue-pressure/dashboard") {
    throw "Detected invalid duplicated route: /api/api/operational/queue-pressure/dashboard"
}

Write-Host "Queue pressure dashboard route check passed."
