param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$SampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "Requesting global operational activity metrics..."
Write-Host "GET $BaseUrl/api/operational/activity/metrics?sampleLimit=$SampleLimit"

$response = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/activity/metrics?sampleLimit=$SampleLimit" `
    -ContentType "application/json"

Write-Host "TotalEventCount: $($response.totalEventCount)"
Write-Host "FirstEventAt: $($response.firstEventAt)"
Write-Host "LastEventAt: $($response.lastEventAt)"
Write-Host "GeneratedAt: $($response.generatedAt)"

if ($response.eventTypes) {
    Write-Host ""
    Write-Host "Event type counts:"
    foreach ($item in $response.eventTypes) {
        Write-Host "- $($item.eventType): $($item.count)"
    }
}

if ($response.sources) {
    Write-Host ""
    Write-Host "Source counts:"
    foreach ($item in $response.sources) {
        Write-Host "- $($item.source): $($item.count)"
    }
}

$response | ConvertTo-Json -Depth 20
