param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

Write-Host "Loading latest operational run..."

$runs = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs" `
    -ContentType "application/json"

if ($runs.Count -eq 0) {
    throw "No operational runs found."
}

$runId = $runs[0].runId

Write-Host "Using run: $runId"

Write-Host ""
Write-Host "Loading projection..."
$projection = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/$runId/status-projection" `
    -ContentType "application/json"

Write-Host ""
Write-Host "Loading control state..."
$controlState = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/$runId/control-state" `
    -ContentType "application/json"

Write-Host ""
Write-Host "Loading dashboard..."
$dashboard = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/$runId/dashboard" `
    -ContentType "application/json"

Write-Host ""
Write-Host "Comparing dashboard with source endpoints..."

if ($dashboard.runId -ne $runId) {
    throw "Dashboard runId does not match selected runId."
}

if ($dashboard.projection.runId -ne $projection.runId) {
    throw "Dashboard projection runId does not match projection endpoint."
}

if ($dashboard.projection.runStatus -ne $projection.runStatus) {
    throw "Dashboard projection runStatus does not match projection endpoint."
}

if ($dashboard.projection.projectionStatus -ne $projection.projectionStatus) {
    throw "Dashboard projectionStatus does not match projection endpoint."
}

if ($dashboard.projection.completionPercent -ne $projection.completionPercent) {
    throw "Dashboard completionPercent does not match projection endpoint."
}

if ($dashboard.controlState.runId -ne $controlState.runId) {
    throw "Dashboard control runId does not match control-state endpoint."
}

if ($dashboard.controlState.currentStatus -ne $controlState.currentStatus) {
    throw "Dashboard control currentStatus does not match control-state endpoint."
}

if ($dashboard.controlState.cancelRequested -ne $controlState.cancelRequested) {
    throw "Dashboard cancelRequested does not match control-state endpoint."
}

if ($dashboard.controlState.aborted -ne $controlState.aborted) {
    throw "Dashboard aborted does not match control-state endpoint."
}

Write-Host "RunStatus: $($dashboard.projection.runStatus)"
Write-Host "ProjectionStatus: $($dashboard.projection.projectionStatus)"
Write-Host "CompletionPercent: $($dashboard.projection.completionPercent)"
Write-Host "ControlStatus: $($dashboard.controlState.currentStatus)"

Write-Host ""
Write-Host "Operational run dashboard consistency smoke passed."
