param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

$url = "$BaseUrl/api/operational/queue-pressure/incident-summary"
$response = Invoke-RestMethod -Uri $url -Method Get

if ($null -eq $response.incidentSummary) {
    throw "Missing incidentSummary root object."
}

if ($response.incidentSummary.totalIncidentStateCount -lt 1) {
    throw "Expected at least one incident summary state."
}

Write-Host "Queue pressure incident-summary smoke test passed."
