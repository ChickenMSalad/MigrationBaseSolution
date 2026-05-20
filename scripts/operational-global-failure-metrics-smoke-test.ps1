param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$SampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "Requesting global operational failure metrics..."
Write-Host "GET $BaseUrl/api/operational/failures/metrics?sampleLimit=$SampleLimit"

$response = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/metrics?sampleLimit=$SampleLimit" `
    -ContentType "application/json"

Write-Host "TotalFailureCount: $($response.totalFailureCount)"
Write-Host "RetriableFailureCount: $($response.retriableFailureCount)"
Write-Host "NonRetriableFailureCount: $($response.nonRetriableFailureCount)"
Write-Host "FirstFailureAt: $($response.firstFailureAt)"
Write-Host "LastFailureAt: $($response.lastFailureAt)"
Write-Host "GeneratedAt: $($response.generatedAt)"

if ($response.failureTypes) {
    Write-Host ""
    Write-Host "Failure type counts:"
    foreach ($item in $response.failureTypes) {
        Write-Host "- $($item.failureType): $($item.count)"
    }
}

$response | ConvertTo-Json -Depth 20
