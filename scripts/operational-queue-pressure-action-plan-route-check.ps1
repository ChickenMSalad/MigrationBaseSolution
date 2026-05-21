param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"
$endpointsUrl = "$BaseUrl/api/system/endpoints"
Write-Host "Checking endpoint discovery: $endpointsUrl"
$response = Invoke-RestMethod -Uri $endpointsUrl -Method Get
$json = $response | ConvertTo-Json -Depth 50

if ($json -notmatch "/api/operational/queue-pressure/action-plan") {
    throw "Expected route not found: /api/operational/queue-pressure/action-plan"
}

if ($json -match "/api/api/operational/queue-pressure/action-plan") {
    throw "Invalid duplicate api route found: /api/api/operational/queue-pressure/action-plan"
}

Write-Host "Route check passed."
