param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"
$url = "$BaseUrl/api/operational/queue-pressure/decision-matrix?pressureLevel=High&recoveryState=Active"

Write-Host "Calling $url"
$response = Invoke-RestMethod -Uri $url -Method Get

if ($null -eq $response.decisionMatrix) {
    throw "Response did not contain decisionMatrix."
}

if ($response.decisionMatrix.isReadOnly -ne $true) {
    throw "decisionMatrix must report read-only mode."
}

if ($null -eq $response.decisionMatrix.summary) {
    throw "decisionMatrix summary was missing."
}

if ($null -eq $response.decisionMatrix.rows -or $response.decisionMatrix.rows.Count -lt 1) {
    throw "decisionMatrix rows were missing."
}

Write-Host "Smoke test passed for /api/operational/queue-pressure/decision-matrix"
