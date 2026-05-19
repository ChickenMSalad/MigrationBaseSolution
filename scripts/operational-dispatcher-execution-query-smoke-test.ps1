param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

Write-Host "Querying latest dispatcher executions..."
$all = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/dispatcher/executions/query?limit=5" `
    -ContentType "application/json"

$all | ConvertTo-Json -Depth 10

Write-Host ""
Write-Host "Querying completed dispatcher executions..."
$completed = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/dispatcher/executions/query?outcome=Completed&limit=5" `
    -ContentType "application/json"

$completed | ConvertTo-Json -Depth 10
