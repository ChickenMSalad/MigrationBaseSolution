param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

$runs = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs" `
    -ContentType "application/json"

if ($runs.Count -eq 0) {
    throw "No operational runs found."
}

$runId = $runs[0].runId

Write-Host "Using run: $runId"

Write-Host ""
Write-Host "Loading per-run timeline catalog..."
$runCatalog = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/$runId/timeline/catalog" `
    -ContentType "application/json"

Write-Host "Loading global timeline catalog..."
$globalCatalog = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/timeline/catalog" `
    -ContentType "application/json"

if ($globalCatalog.eventTypeCount -ne @($globalCatalog.eventTypes).Count) {
    throw "Global catalog eventTypeCount does not match eventTypes length."
}

if ($globalCatalog.sourceCount -ne @($globalCatalog.sources).Count) {
    throw "Global catalog sourceCount does not match sources length."
}

foreach ($eventType in @($runCatalog.eventTypes)) {
    if (-not (@($globalCatalog.eventTypes) -contains $eventType)) {
        throw "Global catalog does not include per-run event type: $eventType"
    }
}

foreach ($source in @($runCatalog.sources)) {
    if (-not (@($globalCatalog.sources) -contains $source)) {
        throw "Global catalog does not include per-run source: $source"
    }
}

Write-Host "PerRunEventTypeCount: $($runCatalog.eventTypeCount)"
Write-Host "GlobalEventTypeCount: $($globalCatalog.eventTypeCount)"
Write-Host "PerRunSourceCount: $($runCatalog.sourceCount)"
Write-Host "GlobalSourceCount: $($globalCatalog.sourceCount)"

Write-Host ""
Write-Host "Global timeline catalog consistency smoke passed."
