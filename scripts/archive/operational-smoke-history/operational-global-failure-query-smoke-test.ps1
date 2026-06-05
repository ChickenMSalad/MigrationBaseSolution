param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$Limit = 10
)

$ErrorActionPreference = "Stop"

Write-Host "Querying global operational failures by limit..."
Write-Host "GET $BaseUrl/api/operational/failures/query?limit=$Limit"

$limited = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/query?limit=$Limit" `
    -ContentType "application/json"

$limited | ConvertTo-Json -Depth 20

if ($limited.count -gt $Limit) {
    throw "Failure query limit was not respected."
}

Write-Host ""
Write-Host "Querying retriable failures..."
$retriable = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/query?isRetriable=true&limit=$Limit" `
    -ContentType "application/json"

$retriable | ConvertTo-Json -Depth 20

foreach ($failure in @($retriable.failures)) {
    if (-not $failure.isRetriable) {
        throw "Retriable filter returned a non-retriable failure."
    }
}

Write-Host ""
Write-Host "Querying failures with search text 'Failure'..."
$searched = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/query?q=Failure&limit=$Limit" `
    -ContentType "application/json"

$searched | ConvertTo-Json -Depth 20

Write-Host ""
Write-Host "Global operational failure query smoke passed."
