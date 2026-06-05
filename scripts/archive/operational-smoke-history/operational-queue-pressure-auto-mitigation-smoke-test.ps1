param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"
$url = "$BaseUrl/api/operational/queue-pressure/auto-mitigation?pressureLevel=Elevated"

Write-Host "Calling $url"
$response = Invoke-RestMethod -Uri $url -Method Get

if ($null -eq $response.autoMitigation) {
    throw "Response did not contain autoMitigation."
}

if ($response.autoMitigation.isAutomaticMutationEnabled -ne $false) {
    throw "autoMitigation must remain guidance-only with automatic mutation disabled."
}

Write-Host "Smoke test passed for /api/operational/queue-pressure/auto-mitigation"
