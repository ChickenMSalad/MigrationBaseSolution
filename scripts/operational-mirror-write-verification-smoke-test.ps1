param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

$url = "$BaseUrl/api/operational/mirror/write-verification"

Write-Host "Requesting operational mirror write verification..."
Write-Host "GET $url"

$response = Invoke-RestMethod `
    -Method Get `
    -Uri $url `
    -ContentType "application/json"

Write-Host "HasRuns: $($response.hasRuns)"
Write-Host "HasManifestRecords: $($response.hasManifestRecords)"
Write-Host "HasWorkItems: $($response.hasWorkItems)"
Write-Host "HasCheckpoints: $($response.hasCheckpoints)"
Write-Host "RunCount: $($response.runCount)"
Write-Host "ManifestRecordCount: $($response.manifestRecordCount)"
Write-Host "WorkItemCount: $($response.workItemCount)"
Write-Host "CheckpointCount: $($response.checkpointCount)"

$response.messages | ForEach-Object {
    Write-Host "- $_"
}

$response | ConvertTo-Json -Depth 10
