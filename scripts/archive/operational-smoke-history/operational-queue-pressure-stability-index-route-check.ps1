param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

$endpointUrl = $BaseUrl.TrimEnd('/') + "/api/system/endpoints"
$response = Invoke-RestMethod -Uri $endpointUrl -Method Get
$json = $response | ConvertTo-Json -Depth 30

if ($json -notmatch "/api/operational/queue-pressure/stability-index") {
    throw "Expected route was not found in endpoint discovery: /api/operational/queue-pressure/stability-index"
}

if ($json -match "/api/api/operational/queue-pressure/stability-index") {
    throw "Unexpected duplicate /api/api route found for queue pressure stability index."
}

Write-Host "Queue pressure stability index route check passed."
