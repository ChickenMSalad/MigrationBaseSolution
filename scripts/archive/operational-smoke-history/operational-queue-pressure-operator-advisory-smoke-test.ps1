param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"
$url = "$BaseUrl/api/operational/queue-pressure/operator-advisory?pressureLevel=High&mode=Active"

Write-Host "Calling $url"
$response = Invoke-RestMethod -Uri $url -Method Get

if ($null -eq $response.operatorAdvisory) {
    throw "Response did not contain operatorAdvisory."
}

if ($response.operatorAdvisory.isReadOnly -ne $true) {
    throw "operatorAdvisory must report read-only mode."
}

if ($null -eq $response.operatorAdvisory.summary) {
    throw "operatorAdvisory summary was missing."
}

Write-Host "Smoke test passed for /api/operational/queue-pressure/operator-advisory"
