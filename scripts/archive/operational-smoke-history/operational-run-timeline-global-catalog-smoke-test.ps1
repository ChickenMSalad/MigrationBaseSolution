param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

Write-Host "Requesting global operational run timeline catalog..."
Write-Host "GET $BaseUrl/api/operational/runs/timeline/catalog"

$response = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/timeline/catalog" `
    -ContentType "application/json"

Write-Host "EventTypeCount: $($response.eventTypeCount)"
Write-Host "SourceCount: $($response.sourceCount)"
Write-Host "GeneratedAt: $($response.generatedAt)"

if (-not ($response.eventTypes -contains "RunCreated")) {
    throw "Global timeline catalog is missing RunCreated."
}

if (-not ($response.eventTypes -contains "WorkItemCreated")) {
    throw "Global timeline catalog is missing WorkItemCreated."
}

if (-not ($response.sources -contains "MigrationRuns")) {
    throw "Global timeline catalog is missing MigrationRuns source."
}

if (-not ($response.sources -contains "MigrationWorkItems")) {
    throw "Global timeline catalog is missing MigrationWorkItems source."
}

$response | ConvertTo-Json -Depth 10
