param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

$url = "$BaseUrl/api/operational/sql/schema/smoke-test"

Write-Host "Requesting operational SQL schema smoke test..."
Write-Host "GET $url"

$response = Invoke-RestMethod `
    -Method Get `
    -Uri $url `
    -ContentType "application/json"

Write-Host "Success: $($response.success)"
Write-Host "ConnectionSucceeded: $($response.connectionSucceeded)"

$response.messages | ForEach-Object {
    Write-Host "- $_"
}

$response | ConvertTo-Json -Depth 10
