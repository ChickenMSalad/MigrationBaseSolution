param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$SampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "Requesting global operational failure catalog..."
Write-Host "GET $BaseUrl/api/operational/failures/catalog?sampleLimit=$SampleLimit"

$response = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/catalog?sampleLimit=$SampleLimit" `
    -ContentType "application/json"

Write-Host "FailureTypeCount: $($response.failureTypeCount)"
Write-Host "RunStatusCount: $($response.runStatusCount)"
Write-Host "SourceSystemCount: $($response.sourceSystemCount)"
Write-Host "TargetSystemCount: $($response.targetSystemCount)"
Write-Host "GeneratedAt: $($response.generatedAt)"

$response | ConvertTo-Json -Depth 10
