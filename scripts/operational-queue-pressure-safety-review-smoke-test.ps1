param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"
$url = "$BaseUrl/api/operational/queue-pressure/safety-review?pressureLevel=Elevated"

Write-Host "Calling $url"
$response = Invoke-RestMethod -Uri $url -Method Get

if ($null -eq $response.safetyReview) {
    throw "Response did not contain safetyReview."
}

if ($response.safetyReview.isReadOnly -ne $true) {
    throw "safetyReview must report read-only mode."
}

if ($response.safetyReview.isAutomaticMutationEnabled -ne $false) {
    throw "safetyReview must keep automatic mutation disabled."
}

Write-Host "Smoke test passed for /api/operational/queue-pressure/safety-review"
