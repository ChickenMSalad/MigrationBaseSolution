param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"
$endpoint = "/api/operational/queue-pressure/capacity-guardrails"
$discoveryUrl = "$BaseUrl/api/system/endpoints"

Write-Host "Checking route discovery for $endpoint"
$endpoints = Invoke-RestMethod -Uri $discoveryUrl -Method Get
$text = $endpoints | ConvertTo-Json -Depth 20

if ($text -notmatch [regex]::Escape($endpoint)) {
    throw "Expected endpoint was not found in /api/system/endpoints: $endpoint"
}

if ($text -match "/api/api/operational/queue-pressure/capacity-guardrails") {
    throw "Invalid duplicated /api/api route found for queue pressure capacity guardrails."
}

Write-Host "Route check passed."
