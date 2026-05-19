param(
    [string]$BaseUrl = "https://localhost:55436",
    [switch]$ArchiveEligible,
    [switch]$PurgeArchived
)

$ErrorActionPreference = "Stop"

Write-Host "Operational retention status:"
$status = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/retention/status" `
    -ContentType "application/json"

$status | ConvertTo-Json -Depth 10

if ($ArchiveEligible) {
    Write-Host ""
    Write-Host "Archiving eligible operational runs..."

    $archive = Invoke-RestMethod `
        -Method Post `
        -Uri "$BaseUrl/api/operational/retention/archive-eligible" `
        -ContentType "application/json"

    $archive | ConvertTo-Json -Depth 10
}

if ($PurgeArchived) {
    Write-Host ""
    Write-Host "Purging archived operational runs..."

    $purge = Invoke-RestMethod `
        -Method Post `
        -Uri "$BaseUrl/api/operational/retention/purge-archived" `
        -ContentType "application/json"

    $purge | ConvertTo-Json -Depth 10
}
