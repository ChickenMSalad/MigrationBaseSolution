param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"
$endpoint = "/api/operational/queue-pressure/decision-matrix"
$discoveryUrl = "$BaseUrl/api/system/endpoints"

Write-Host "Checking endpoint discovery for $endpoint"
$response = Invoke-RestMethod -Uri $discoveryUrl -Method Get
$json = $response | ConvertTo-Json -Depth 20

if ($json -notlike "*$endpoint*") {
    throw "Expected endpoint was not found in endpoint discovery: $endpoint"
}

if ($json -like "*/api/api/operational/queue-pressure/decision-matrix*") {
    throw "Detected accidental /api/api route for queue pressure decision matrix."
}

Write-Host "Route check passed for $endpoint"
