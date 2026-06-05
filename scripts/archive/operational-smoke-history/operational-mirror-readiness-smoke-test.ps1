param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

$url = "$BaseUrl/api/operational/mirror/readiness"

Write-Host "Requesting operational mirror readiness..."
Write-Host "GET $url"

$response = Invoke-RestMethod `
    -Method Get `
    -Uri $url `
    -ContentType "application/json"

Write-Host "Ready: $($response.ready)"
Write-Host "Enabled: $($response.enabled)"
Write-Host "MirrorServiceRegistered: $($response.mirrorServiceRegistered)"
Write-Host "OptionsValidatorRegistered: $($response.optionsValidatorRegistered)"
Write-Host "OperationalStoreRegistered: $($response.operationalStoreRegistered)"

$response | ConvertTo-Json -Depth 10
