param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

$endpoint = "$BaseUrl/api/system/endpoints"
$response = Invoke-RestMethod -Uri $endpoint -Method Get
$json = $response | ConvertTo-Json -Depth 20

if ($json -notmatch "/api/operational/queue-pressure/recovery-workflow") {
    throw "Expected route was not found: /api/operational/queue-pressure/recovery-workflow"
}

if ($json -match "/api/api/operational/queue-pressure/recovery-workflow") {
    throw "Invalid duplicate /api/api route was found."
}

Write-Host "Queue pressure recovery workflow route check passed."
