param(
    [string]$BaseUrl = "https://localhost:55436",
    [switch]$RunOnce
)

$ErrorActionPreference = "Stop"

Write-Host "Requesting operational auto-finalization status..."
$status = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/auto-finalization/status" `
    -ContentType "application/json"

$status | ConvertTo-Json -Depth 10

if ($RunOnce) {
    Write-Host ""
    Write-Host "Running auto-finalization once..."

    $result = Invoke-RestMethod `
        -Method Post `
        -Uri "$BaseUrl/api/operational/runs/auto-finalization/run-once" `
        -ContentType "application/json"

    $result | ConvertTo-Json -Depth 10
}

Write-Host ""
Write-Host "Operational projection:"
./scripts/operational-run-status-projection-smoke-test.ps1 `
    -BaseUrl $BaseUrl
