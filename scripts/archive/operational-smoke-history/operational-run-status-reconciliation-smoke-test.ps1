param(
    [string]$BaseUrl = "https://localhost:55436",
    [switch]$Apply
)

$ErrorActionPreference = "Stop"
$runs = Invoke-RestMethod -Method Get -Uri "$BaseUrl/api/operational/runs" -ContentType "application/json"
if ($runs.Count -eq 0) { throw "No operational runs found." }
$runId = $runs[0].runId
Write-Host "Using run: $runId"

$previewUrl = "$BaseUrl/api/operational/runs/$runId/status-reconciliation"
Write-Host ""
Write-Host "Previewing reconciliation..."
Write-Host "GET $previewUrl"
$preview = Invoke-RestMethod -Method Get -Uri $previewUrl -ContentType "application/json"
$preview | ConvertTo-Json -Depth 10

if (-not $Apply) {
    Write-Host ""
    Write-Host "Apply switch was not specified. Preview only."
    exit 0
}

$applyUrl = "$BaseUrl/api/operational/runs/$runId/reconcile-status"
Write-Host ""
Write-Host "Applying reconciliation..."
Write-Host "POST $applyUrl"
$applied = Invoke-RestMethod -Method Post -Uri $applyUrl -ContentType "application/json"
$applied | ConvertTo-Json -Depth 10

Write-Host ""
Write-Host "Projection after reconciliation:"
./scripts/operational-run-status-projection-smoke-test.ps1 -BaseUrl $BaseUrl
