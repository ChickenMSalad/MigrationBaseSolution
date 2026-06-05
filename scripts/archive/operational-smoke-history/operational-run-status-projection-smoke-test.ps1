param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

$listUrl = "$BaseUrl/api/operational/runs/status-projections"

Write-Host "Requesting operational run status projections..."
Write-Host "GET $listUrl"

$projections = Invoke-RestMethod `
    -Method Get `
    -Uri $listUrl `
    -ContentType "application/json"

Write-Host "Projection count: $($projections.Count)"

if ($projections.Count -eq 0) {
    Write-Host "No operational run projections found."
    exit 0
}

$latest = $projections[0]

Write-Host ""
Write-Host "Latest projection:"
Write-Host "RunId: $($latest.runId)"
Write-Host "RunStatus: $($latest.runStatus)"
Write-Host "ProjectionStatus: $($latest.projectionStatus)"
Write-Host "CompletionPercent: $($latest.completionPercent)"
Write-Host "ManifestRecordCount: $($latest.manifestRecordCount)"
Write-Host "WorkItemCount: $($latest.workItemCount)"
Write-Host "FailureCount: $($latest.failureCount)"
Write-Host "CheckpointCount: $($latest.checkpointCount)"

$detailUrl = "$BaseUrl/api/operational/runs/$($latest.runId)/status-projection"

Write-Host ""
Write-Host "GET $detailUrl"

$detail = Invoke-RestMethod `
    -Method Get `
    -Uri $detailUrl `
    -ContentType "application/json"

$detail | ConvertTo-Json -Depth 10
