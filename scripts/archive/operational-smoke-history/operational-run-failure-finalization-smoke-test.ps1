param(
    [string]$BaseUrl = "https://localhost:55436",
    [switch]$Finalize
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

$readinessUrl = "$BaseUrl/api/operational/runs/$runId/failure-readiness"

Write-Host ""
Write-Host "Failure readiness:"
Write-Host "GET $readinessUrl"

$readiness = Invoke-RestMethod `
    -Method Get `
    -Uri $readinessUrl `
    -ContentType "application/json"

$readiness | ConvertTo-Json -Depth 10

if (-not $Finalize) {
    Write-Host ""
    Write-Host "Finalize switch was not specified. Readiness only."
    exit 0
}

$finalizeUrl = "$BaseUrl/api/operational/runs/$runId/finalize-failure"

Write-Host ""
Write-Host "Finalizing failure:"
Write-Host "POST $finalizeUrl"

$finalized = Invoke-RestMethod `
    -Method Post `
    -Uri $finalizeUrl `
    -ContentType "application/json"

$finalized | ConvertTo-Json -Depth 10

Write-Host ""
Write-Host "Projection after failure finalization attempt:"
./scripts/operational-run-status-projection-smoke-test.ps1 `
    -BaseUrl $BaseUrl
