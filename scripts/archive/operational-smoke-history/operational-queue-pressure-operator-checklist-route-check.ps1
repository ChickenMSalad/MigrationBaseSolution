param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"
$endpointUrl = "$BaseUrl/api/system/endpoints"
$response = Invoke-RestMethod -Uri $endpointUrl -Method Get
$json = $response | ConvertTo-Json -Depth 20

if ($json -notmatch "/api/operational/queue-pressure/operator-checklist") {
    throw "Missing expected route: /api/operational/queue-pressure/operator-checklist"
}

if ($json -match "/api/api/operational/queue-pressure/operator-checklist") {
    throw "Unexpected duplicate route found: /api/api/operational/queue-pressure/operator-checklist"
}

Write-Host "Queue pressure operator-checklist route check passed."
