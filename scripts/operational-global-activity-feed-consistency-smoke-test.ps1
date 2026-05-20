param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$Limit = 10
)

$ErrorActionPreference = "Stop"

Write-Host "Loading global activity feed..."
$response = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/activity/recent?limit=$Limit" `
    -ContentType "application/json"

$events = @($response.events)

if ($response.limit -ne $Limit) {
    throw "Activity feed response limit does not match requested limit."
}

if ($response.eventCount -ne $events.Count) {
    throw "Activity feed eventCount does not match events array length."
}

if ($response.eventCount -gt $Limit) {
    throw "Activity feed returned more events than requested limit."
}

for ($i = 1; $i -lt $events.Count; $i++) {
    $previous = [DateTimeOffset]$events[$i - 1].occurredAt
    $current = [DateTimeOffset]$events[$i].occurredAt

    if ($current -gt $previous) {
        throw "Activity feed is not sorted descending by occurredAt at index $i."
    }
}

$allowedSources = @(
    "MigrationRuns",
    "MigrationWorkItems",
    "MigrationCheckpoints",
    "MigrationFailures"
)

foreach ($event in $events) {
    if (-not $event.runId) {
        throw "Activity feed event is missing runId."
    }

    if (-not $event.eventType) {
        throw "Activity feed event is missing eventType."
    }

    if (-not $event.source) {
        throw "Activity feed event is missing source."
    }

    if (-not ($allowedSources -contains $event.source)) {
        throw "Activity feed returned unexpected source: $($event.source)"
    }

    if (-not $event.message) {
        throw "Activity feed event is missing message."
    }
}

Write-Host "EventCount: $($response.eventCount)"
Write-Host "Limit: $($response.limit)"
Write-Host "GeneratedAt: $($response.generatedAt)"

Write-Host ""
Write-Host "Global operational activity feed consistency smoke passed."
