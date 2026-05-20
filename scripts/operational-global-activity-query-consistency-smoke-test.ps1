param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$Limit = 10
)

$ErrorActionPreference = "Stop"

Write-Host "Loading recent activity feed..."
$feed = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/activity/recent?limit=100" `
    -ContentType "application/json"

Write-Host "Loading limited activity query..."
$query = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/activity/query?limit=$Limit" `
    -ContentType "application/json"

if ($query.limit -ne $Limit) {
    throw "Query limit does not match requested limit."
}

if ($query.eventCount -ne @($query.events).Count) {
    throw "Query eventCount does not match events array length."
}

if ($query.eventCount -gt $Limit) {
    throw "Query returned more events than requested limit."
}

$feedKeys = @{}
foreach ($event in @($feed.events)) {
    $key = "$($event.runId)|$($event.occurredAt)|$($event.eventType)|$($event.source)|$($event.message)"
    $feedKeys[$key] = $true
}

foreach ($event in @($query.events)) {
    $key = "$($event.runId)|$($event.occurredAt)|$($event.eventType)|$($event.source)|$($event.message)"

    if (-not $feedKeys.ContainsKey($key)) {
        throw "Query returned an event that was not found in the recent activity feed window."
    }
}

Write-Host ""
Write-Host "Loading source-filtered activity query..."
$sourceQuery = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/activity/query?source=MigrationRuns&limit=$Limit" `
    -ContentType "application/json"

foreach ($event in @($sourceQuery.events)) {
    if ($event.source -ne "MigrationRuns") {
        throw "Source filtered query returned a non-MigrationRuns event."
    }
}

Write-Host ""
Write-Host "Loading search-filtered activity query..."
$searchQuery = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/activity/query?q=Run&limit=$Limit" `
    -ContentType "application/json"

foreach ($event in @($searchQuery.events)) {
    $haystack = "$($event.eventType) $($event.source) $($event.message) $($event.runId) $($event.workItemId) $($event.manifestRecordId) $($event.checkpointId) $($event.failureId)"

    if ($haystack.IndexOf("Run", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Search filtered query returned an event that does not match search text."
    }
}

Write-Host "BaseQueryEventCount: $($query.eventCount)"
Write-Host "SourceQueryEventCount: $($sourceQuery.eventCount)"
Write-Host "SearchQueryEventCount: $($searchQuery.eventCount)"

Write-Host ""
Write-Host "Global operational activity query consistency smoke passed."
