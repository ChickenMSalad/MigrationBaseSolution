param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"
$url = "$BaseUrl/api/operational/queue-pressure/risk-banding?pressureLevel=High&queueTrend=Worsening&dispatcherState=Constrained"

Write-Host "Calling $url"
$response = Invoke-RestMethod -Uri $url -Method Get

if ($null -eq $response.riskBanding) {
    throw "Response did not contain riskBanding."
}

if ($response.riskBanding.isReadOnly -ne $true) {
    throw "riskBanding must report read-only mode."
}

if ($null -eq $response.riskBanding.summary) {
    throw "riskBanding summary was missing."
}

if ($null -eq $response.riskBanding.bands -or $response.riskBanding.bands.Count -lt 1) {
    throw "riskBanding bands were missing."
}

Write-Host "Smoke test passed for /api/operational/queue-pressure/risk-banding"
