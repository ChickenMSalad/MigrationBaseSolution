param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

$url = $BaseUrl.TrimEnd('/') + "/api/operational/queue-pressure/stability-index"
$response = Invoke-RestMethod -Uri $url -Method Get

if ($null -eq $response.stabilityIndex) {
    throw "Response did not include stabilityIndex root object."
}

if ([string]::IsNullOrWhiteSpace($response.stabilityIndex.endpoint)) {
    throw "Response did not include stabilityIndex.endpoint."
}

if ($null -eq $response.stabilityIndex.stabilityBands -or $response.stabilityIndex.stabilityBands.Count -lt 1) {
    throw "Response did not include stability bands."
}

Write-Host "Queue pressure stability index smoke test passed."
