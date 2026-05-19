param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

$runs = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs" `
    -ContentType "application/json"

if ($runs.Count -eq 0) {
    throw "No operational runs found."
}

$runId = $runs[0].runId

Write-Host "Using run: $runId"
Write-Host "GET $BaseUrl/api/operational/runs/$runId/dashboard"

$response = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/$runId/dashboard" `
    -ContentType "application/json"

Write-Host "RunStatus: $($response.projection.runStatus)"
Write-Host "ProjectionStatus: $($response.projection.projectionStatus)"
Write-Host "CompletionPercent: $($response.projection.completionPercent)"
Write-Host "CancelRequested: $($response.controlState.cancelRequested)"
Write-Host "Aborted: $($response.controlState.aborted)"
Write-Host "CanFinalizeCompletion: $($response.completionReadiness.canFinalize)"
Write-Host "CanFinalizeFailure: $($response.failureReadiness.canFinalizeFailure)"

if ($response.messages) {
    Write-Host ""
    Write-Host "Messages:"
    foreach ($message in $response.messages) {
        Write-Host "- $message"
    }
}

$response | ConvertTo-Json -Depth 20
