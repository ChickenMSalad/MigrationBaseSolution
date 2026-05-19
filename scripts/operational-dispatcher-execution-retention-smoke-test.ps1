param(
    [string]$BaseUrl = "https://localhost:55436",
    [switch]$PurgeEligible
)

$ErrorActionPreference = "Stop"

Write-Host "Dispatcher execution history retention status:"
$status = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/dispatcher/executions/retention/status" `
    -ContentType "application/json"

$status | ConvertTo-Json -Depth 10

if ($PurgeEligible) {
    Write-Host ""
    Write-Host "Purging eligible dispatcher execution history..."

    $purge = Invoke-RestMethod `
        -Method Post `
        -Uri "$BaseUrl/api/operational/dispatcher/executions/retention/purge-eligible" `
        -ContentType "application/json"

    $purge | ConvertTo-Json -Depth 10
}
