param([string]$BaseUrl = "https://localhost:55436")
$ErrorActionPreference = "Stop"
$endpoint = "$BaseUrl/api/system/endpoints"
$response = Invoke-RestMethod -Uri $endpoint -Method Get
$text = $response | ConvertTo-Json -Depth 20
if ($text -notmatch "/api/operational/queue-pressure/finalization") { throw "Route not found: /api/operational/queue-pressure/finalization" }
if ($text -match "/api/api/operational/queue-pressure/finalization") { throw "Invalid duplicate /api/api route found." }
Write-Host "Route check passed for operational queue pressure finalization."
