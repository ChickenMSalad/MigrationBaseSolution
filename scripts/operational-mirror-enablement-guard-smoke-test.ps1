param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

$url = "$BaseUrl/api/operational/mirror/enablement-guard"

Write-Host "Requesting operational mirror enablement guard..."
Write-Host "GET $url"

$response = Invoke-RestMethod `
    -Method Get `
    -Uri $url `
    -ContentType "application/json"

Write-Host "CanMirror: $($response.canMirror)"
Write-Host "MirrorEnabled: $($response.mirrorEnabled)"
Write-Host "ReadinessPassed: $($response.readinessPassed)"
Write-Host "SqlSchemaPassed: $($response.sqlSchemaPassed)"

$response.messages | ForEach-Object {
    Write-Host "- $_"
}

$response | ConvertTo-Json -Depth 10
