param(
    [string]$BaseUrl = "https://localhost:55436",
    [string]$WorkerId = "local-smoke-worker",
    [int]$Count = 1,
    [switch]$CompleteLeasedItem
)

$ErrorActionPreference = "Stop"

$leaseUrl = "$BaseUrl/api/operational/work-items/lease"

$body = @{
    workerId = $WorkerId
    count = $Count
} | ConvertTo-Json -Depth 5

Write-Host "Requesting operational work-item lease..."
Write-Host "POST $leaseUrl"

$lease = Invoke-RestMethod `
    -Method Post `
    -Uri $leaseUrl `
    -ContentType "application/json" `
    -Body $body

Write-Host "RequestedCount: $($lease.requestedCount)"
Write-Host "LeasedCount: $($lease.leasedCount)"

$lease | ConvertTo-Json -Depth 10

if ($lease.leasedCount -eq 0) {
    Write-Host "No work items were leased."
    exit 0
}

$workItem = $lease.workItems[0]
$workItemId = $workItem.workItemId

Write-Host ""
Write-Host "Sending heartbeat for work item $workItemId"

$heartbeatUrl = "$BaseUrl/api/operational/work-items/$workItemId/heartbeat"
$heartbeatBody = @{
    workerId = $WorkerId
} | ConvertTo-Json -Depth 5

$heartbeat = Invoke-RestMethod `
    -Method Post `
    -Uri $heartbeatUrl `
    -ContentType "application/json" `
    -Body $heartbeatBody

$heartbeat | ConvertTo-Json -Depth 10

if ($CompleteLeasedItem) {
    Write-Host ""
    Write-Host "Completing work item $workItemId"

    $completeUrl = "$BaseUrl/api/operational/work-items/$workItemId/complete"
    $completeBody = @{
        workerId = $WorkerId
    } | ConvertTo-Json -Depth 5

    $complete = Invoke-RestMethod `
        -Method Post `
        -Uri $completeUrl `
        -ContentType "application/json" `
        -Body $completeBody

    $complete | ConvertTo-Json -Depth 10
}
else {
    Write-Host ""
    Write-Host "CompleteLeasedItem was not specified. Work item remains locked for inspection."
}
