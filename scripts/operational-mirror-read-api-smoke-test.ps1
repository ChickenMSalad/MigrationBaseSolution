param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

$runsUrl = "$BaseUrl/api/operational/runs"

Write-Host "Requesting operational runs..."
Write-Host "GET $runsUrl"

$runs = Invoke-RestMethod `
    -Method Get `
    -Uri $runsUrl `
    -ContentType "application/json"

Write-Host "Operational run count: $($runs.Count)"

if ($runs.Count -eq 0) {
    Write-Host "No operational runs found."
    exit 0
}

$latest = $runs[0]

Write-Host ""
Write-Host "Latest operational run:"
Write-Host "RunId: $($latest.runId)"
Write-Host "Status: $($latest.status)"
Write-Host "ManifestRecordCount: $($latest.manifestRecordCount)"
Write-Host "WorkItemCount: $($latest.workItemCount)"
Write-Host "CheckpointCount: $($latest.checkpointCount)"

$detailUrl = "$BaseUrl/api/operational/runs/$($latest.runId)"

Write-Host ""
Write-Host "GET $detailUrl"

$detail = Invoke-RestMethod `
    -Method Get `
    -Uri $detailUrl `
    -ContentType "application/json"

$detail | ConvertTo-Json -Depth 10
