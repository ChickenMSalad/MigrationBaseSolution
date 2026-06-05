param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$SampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "Loading activity feed sample..."
$feed = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/activity/recent?limit=$SampleLimit" `
    -ContentType "application/json"

Write-Host "Loading activity metrics..."
$metrics = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/activity/metrics?sampleLimit=$SampleLimit" `
    -ContentType "application/json"

$events = @($feed.events)

if ($metrics.totalEventCount -ne $events.Count) {
    throw "Metrics totalEventCount does not match feed event count."
}

if ($events.Count -gt 0) {
    $expectedFirst = ($events | Sort-Object occurredAt | Select-Object -First 1).occurredAt
    $expectedLast = ($events | Sort-Object occurredAt -Descending | Select-Object -First 1).occurredAt

    if ($metrics.firstEventAt -ne $expectedFirst) {
        throw "Metrics firstEventAt does not match feed sample."
    }

    if ($metrics.lastEventAt -ne $expectedLast) {
        throw "Metrics lastEventAt does not match feed sample."
    }
}

$eventTypeCounts = @{}
foreach ($event in $events) {
    if (-not $eventTypeCounts.ContainsKey($event.eventType)) {
        $eventTypeCounts[$event.eventType] = 0
    }

    $eventTypeCounts[$event.eventType]++
}

foreach ($item in @($metrics.eventTypes)) {
    if (-not $eventTypeCounts.ContainsKey($item.eventType)) {
        throw "Metrics returned event type not found in feed sample: $($item.eventType)"
    }

    if ($eventTypeCounts[$item.eventType] -ne $item.count) {
        throw "Metrics count mismatch for event type $($item.eventType)."
    }
}

$sourceCounts = @{}
foreach ($event in $events) {
    if (-not $sourceCounts.ContainsKey($event.source)) {
        $sourceCounts[$event.source] = 0
    }

    $sourceCounts[$event.source]++
}

foreach ($item in @($metrics.sources)) {
    if (-not $sourceCounts.ContainsKey($item.source)) {
        throw "Metrics returned source not found in feed sample: $($item.source)"
    }

    if ($sourceCounts[$item.source] -ne $item.count) {
        throw "Metrics count mismatch for source $($item.source)."
    }
}

Write-Host "TotalEventCount: $($metrics.totalEventCount)"
Write-Host "EventTypeMetricCount: $(@($metrics.eventTypes).Count)"
Write-Host "SourceMetricCount: $(@($metrics.sources).Count)"
Write-Host ""
Write-Host "Global operational activity metrics consistency smoke passed."
