param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

$url = "$BaseUrl/api/operational/mirror/status"

Write-Host "Requesting operational mirror configuration status..."
Write-Host "GET $url"

$response = Invoke-RestMethod `
    -Method Get `
    -Uri $url `
    -ContentType "application/json"

Write-Host "Mode: $($response.mode)"
Write-Host "Enabled: $($response.enabled)"
Write-Host "MirrorServiceRegistered: $($response.mirrorServiceRegistered)"
Write-Host "OptionsValidatorRegistered: $($response.optionsValidatorRegistered)"

$response | ConvertTo-Json -Depth 10
