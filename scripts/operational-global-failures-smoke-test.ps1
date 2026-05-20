param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$Limit = 10
)

$ErrorActionPreference = "Stop"

Write-Host "Requesting recent global operational failures..."
Write-Host "GET $BaseUrl/api/operational/failures/recent?limit=$Limit"

$response = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/recent?limit=$Limit" `
    -ContentType "application/json"

Write-Host "Count: $($response.count)"
Write-Host "Limit: $($response.limit)"
Write-Host "GeneratedAt: $($response.generatedAt)"

if ($response.count -gt $Limit) {
    throw "Recent failures limit was not respected."
}

if ($response.failures) {
    Write-Host ""
    Write-Host "Failure preview:"
    foreach ($failure in @($response.failures | Select-Object -First 10)) {
        Write-Host "$($failure.createdAt) [$($failure.failureType)] Run=$($failure.runId) $($failure.message)"
    }
}

$response | ConvertTo-Json -Depth 20
