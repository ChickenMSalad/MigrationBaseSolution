param(
    [string]$BaseUrl = "https://localhost:55436",
    [string]$WorkerId = "local-smoke-worker",
    [switch]$ResetCompletedItem
)

$ErrorActionPreference = "Stop"

Write-Host "Requesting operational runs..."
$runs = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs" `
    -ContentType "application/json"

if ($runs.Count -eq 0) {
    Write-Host "No operational runs found."
    exit 0
}

$runId = $runs[0].runId

Write-Host "Using latest run: $runId"

$detail = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/$runId" `
    -ContentType "application/json"

if ($detail.workItems.Count -eq 0) {
    Write-Host "No work items found."
    exit 0
}

$workItem = $detail.workItems[0]
$workItemId = $workItem.workItemId

Write-Host "Using work item: $workItemId"
Write-Host "Current status: $($workItem.status)"

if ($workItem.status -eq "Locked") {
    Write-Host "Releasing locked work item..."

    $body = @{
        workerId = $WorkerId
    } | ConvertTo-Json -Depth 5

    $release = Invoke-RestMethod `
        -Method Post `
        -Uri "$BaseUrl/api/operational/work-items/$workItemId/release" `
        -ContentType "application/json" `
        -Body $body

    $release | ConvertTo-Json -Depth 10
}
elseif ($ResetCompletedItem -or $workItem.status -ne "Created") {
    Write-Host "Resetting work item to Created..."

    $body = @{
        reason = "Local smoke reset from status $($workItem.status)."
    } | ConvertTo-Json -Depth 5

    $reset = Invoke-RestMethod `
        -Method Post `
        -Uri "$BaseUrl/api/operational/work-items/$workItemId/reset" `
        -ContentType "application/json" `
        -Body $body

    $reset | ConvertTo-Json -Depth 10
}
else {
    Write-Host "Work item is already Created. Nothing to release/reset."
}

Write-Host ""
Write-Host "Projection after recovery action:"
./scripts/operational-run-status-projection-smoke-test.ps1 `
    -BaseUrl $BaseUrl
