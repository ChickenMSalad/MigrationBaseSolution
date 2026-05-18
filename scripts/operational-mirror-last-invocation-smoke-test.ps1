param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

$url = "$BaseUrl/api/operational/mirror/last-invocation"

Write-Host "Requesting operational mirror last invocation..."
Write-Host "GET $url"

$response = Invoke-RestMethod `
    -Method Get `
    -Uri $url `
    -ContentType "application/json"

Write-Host "Invoked: $($response.invoked)"
Write-Host "Mirrored: $($response.mirrored)"
Write-Host "Failed: $($response.failed)"
Write-Host "LegacyRunId: $($response.legacyRunId)"
Write-Host "Message: $($response.message)"
Write-Host "RecordedAt: $($response.recordedAt)"

$response | ConvertTo-Json -Depth 10
