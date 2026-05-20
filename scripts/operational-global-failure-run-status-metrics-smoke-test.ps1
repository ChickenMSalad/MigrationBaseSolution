param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$SampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "Requesting global operational failure run-status metrics..."
Write-Host "GET $BaseUrl/api/operational/failures/run-status-metrics?sampleLimit=$SampleLimit"

$response = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/run-status-metrics?sampleLimit=$SampleLimit" `
    -ContentType "application/json"

Write-Host "TotalFailureCount: $($response.totalFailureCount)"
Write-Host "RunStatusCount: $($response.runStatusCount)"
Write-Host "GeneratedAt: $($response.generatedAt)"

if ($response.runStatuses) {
    Write-Host ""
    Write-Host "Run status failure counts:"
    foreach ($item in $response.runStatuses) {
        Write-Host "- $($item.runStatus): $($item.count)"
    }
}

$response | ConvertTo-Json -Depth 25
