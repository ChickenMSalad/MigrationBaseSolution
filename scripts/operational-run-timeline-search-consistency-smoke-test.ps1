param(
    [string]$BaseUrl = "https://localhost:55436",
    [string]$SearchText = "WorkItem",
    [int]$Limit = 10
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
Write-Host "SearchText: $SearchText"
Write-Host "Limit: $Limit"

Write-Host ""
Write-Host "Loading full timeline..."
$timeline = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/$runId/timeline" `
    -ContentType "application/json"

Write-Host "FullTimelineEventCount: $($timeline.eventCount)"

Write-Host ""
Write-Host "Searching timeline..."
$encodedSearchText = [System.Web.HttpUtility]::UrlEncode($SearchText)

$search = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/runs/$runId/timeline/search?q=$encodedSearchText&limit=$Limit" `
    -ContentType "application/json"

Write-Host "SearchEventCount: $($search.eventCount)"

if ($search.runId -ne $runId) {
    throw "Search runId does not match selected runId."
}

if ($search.eventCount -ne @($search.events).Count) {
    throw "Search eventCount does not match event array length."
}

if ($search.eventCount -gt $Limit) {
    throw "Timeline search limit was not respected."
}

$fullEventKeys = @{}
foreach ($event in @($timeline.events)) {
    $key = "$($event.occurredAt)|$($event.eventType)|$($event.source)|$($event.message)"
    $fullEventKeys[$key] = $true
}

foreach ($event in @($search.events)) {
    $key = "$($event.occurredAt)|$($event.eventType)|$($event.source)|$($event.message)"

    if (-not $fullEventKeys.ContainsKey($key)) {
        throw "Search returned an event that does not exist in the full timeline."
    }

    $haystack = "$($event.eventType) $($event.source) $($event.message) $($event.workItemId) $($event.manifestRecordId) $($event.checkpointId) $($event.failureId)"

    if ($haystack.IndexOf($SearchText, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Search returned an event that does not match the search text."
    }
}

Write-Host ""
Write-Host "Timeline search consistency smoke passed."
