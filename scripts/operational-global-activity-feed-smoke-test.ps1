param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$Limit = 10
)

$ErrorActionPreference = "Stop"

Write-Host "Requesting recent global operational activity..."
Write-Host "GET $BaseUrl/api/operational/activity/recent?limit=$Limit"

$response = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/activity/recent?limit=$Limit" `
    -ContentType "application/json"

Write-Host "EventCount: $($response.eventCount)"
Write-Host "Limit: $($response.limit)"
Write-Host "GeneratedAt: $($response.generatedAt)"

if ($response.eventCount -gt $Limit) {
    throw "Activity feed limit was not respected."
}

if ($response.events) {
    Write-Host ""
    Write-Host "Recent activity preview:"
    foreach ($event in @($response.events | Select-Object -First 10)) {
        Write-Host "$($event.occurredAt) [$($event.eventType)] Run=$($event.runId) $($event.message)"
    }
}

$response | ConvertTo-Json -Depth 20
