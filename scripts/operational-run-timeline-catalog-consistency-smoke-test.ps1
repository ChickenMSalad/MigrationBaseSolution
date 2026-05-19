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
Write-Host "Loading timeline metrics..."
$metrics = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/$runId/timeline/metrics" `
    -ContentType "application/json"

Write-Host "Loading timeline catalog..."
$catalog = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/$runId/timeline/catalog" `
    -ContentType "application/json"

if ($catalog.runId -ne $runId) {
    throw "Catalog runId does not match selected runId."
}

$metricEventTypes = @($metrics.eventTypes | ForEach-Object { $_.eventType } | Sort-Object)
$catalogEventTypes = @($catalog.eventTypes | Sort-Object)

$metricSources = @($metrics.sources | ForEach-Object { $_.source } | Sort-Object)
$catalogSources = @($catalog.sources | Sort-Object)

if ($catalog.eventTypeCount -ne $catalogEventTypes.Count) {
    throw "Catalog eventTypeCount does not match eventTypes length."
}

if ($catalog.sourceCount -ne $catalogSources.Count) {
    throw "Catalog sourceCount does not match sources length."
}

if (($metricEventTypes -join "|") -ne ($catalogEventTypes -join "|")) {
    throw "Catalog event types do not match timeline metrics event types."
}

if (($metricSources -join "|") -ne ($catalogSources -join "|")) {
    throw "Catalog sources do not match timeline metrics sources."
}

Write-Host "EventTypeCount: $($catalog.eventTypeCount)"
Write-Host "SourceCount: $($catalog.sourceCount)"

Write-Host ""
Write-Host "Timeline catalog consistency smoke passed."
