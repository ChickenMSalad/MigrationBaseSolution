param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"
$endpoint = "/api/operational/queue-pressure/recovery-readiness"
$discoveryUrl = "$BaseUrl/api/system/endpoints"

Write-Host "Checking endpoint discovery for $endpoint"
$response = Invoke-RestMethod -Uri $discoveryUrl -Method Get
$json = $response | ConvertTo-Json -Depth 20

if ($json -notlike "*$endpoint*") {
    throw "Expected endpoint was not found in endpoint discovery: $endpoint"
}

if ($json -like "*/api/api/operational/queue-pressure/recovery-readiness*") {
    throw "Detected accidental /api/api route for queue pressure recovery readiness."
}

Write-Host "Route check passed for $endpoint"
