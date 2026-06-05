param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

$endpoint = "$BaseUrl/api/system/endpoints"
$response = Invoke-RestMethod -Uri $endpoint -Method Get
$json = $response | ConvertTo-Json -Depth 20

if ($json -notmatch "/api/operational/queue-pressure/incident-summary") {
    throw "Expected route was not found: /api/operational/queue-pressure/incident-summary"
}

if ($json -match "/api/api/operational/queue-pressure/incident-summary") {
    throw "Invalid duplicate /api/api route was found."
}

Write-Host "Queue pressure incident-summary route check passed."
