param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$SampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "Requesting global operational failure system-pair metrics..."
Write-Host "GET $BaseUrl/api/operational/failures/system-pair-metrics?sampleLimit=$SampleLimit"

$response = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/system-pair-metrics?sampleLimit=$SampleLimit" `
    -ContentType "application/json"

Write-Host "TotalFailureCount: $($response.totalFailureCount)"
Write-Host "SystemPairCount: $($response.systemPairCount)"
Write-Host "GeneratedAt: $($response.generatedAt)"

if ($response.systemPairs) {
    Write-Host ""
    Write-Host "System pair failure counts:"
    foreach ($item in $response.systemPairs) {
        Write-Host "- $($item.sourceSystem) -> $($item.targetSystem): $($item.count)"
    }
}

$response | ConvertTo-Json -Depth 25
