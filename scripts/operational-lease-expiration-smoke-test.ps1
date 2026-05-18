param(
    [string]$BaseUrl = "https://localhost:55436",
    [switch]$Reclaim
)

$ErrorActionPreference = "Stop"

$listUrl = "$BaseUrl/api/operational/work-items/expired-leases"

Write-Host "Requesting expired operational leases..."
Write-Host "GET $listUrl"

$expired = Invoke-RestMethod `
    -Method Get `
    -Uri $listUrl `
    -ContentType "application/json"

Write-Host "LeaseTimeoutMinutes: $($expired.leaseTimeoutMinutes)"
Write-Host "ExpiresBefore: $($expired.expiresBefore)"
Write-Host "ExpiredCount: $($expired.count)"

$expired | ConvertTo-Json -Depth 10

if (-not $Reclaim) {
    Write-Host ""
    Write-Host "Reclaim switch was not specified. Detection only."
    exit 0
}

Write-Host ""
Write-Host "Reclaiming expired leases..."

$reclaimUrl = "$BaseUrl/api/operational/work-items/reclaim-expired"
$body = @{
    maxCount = 100
    reason = "Local smoke reclaim of expired operational leases."
} | ConvertTo-Json -Depth 5

$reclaimed = Invoke-RestMethod `
    -Method Post `
    -Uri $reclaimUrl `
    -ContentType "application/json" `
    -Body $body

$reclaimed | ConvertTo-Json -Depth 10
