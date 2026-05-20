param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$Limit = 5
)

$ErrorActionPreference = "Stop"

Write-Host "Querying global activity by limit..."
$limited = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/activity/query?limit=$Limit" `
    -ContentType "application/json"

$limited | ConvertTo-Json -Depth 20

if ($limited.eventCount -gt $Limit) {
    throw "Global activity query limit was not respected."
}

Write-Host ""
Write-Host "Querying global activity for WorkItem events..."
$workItems = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/activity/query?q=WorkItem&limit=10" `
    -ContentType "application/json"

$workItems | ConvertTo-Json -Depth 20

foreach ($event in @($workItems.events)) {
    $haystack = "$($event.eventType) $($event.source) $($event.message) $($event.runId) $($event.workItemId) $($event.manifestRecordId) $($event.checkpointId) $($event.failureId)"

    if ($haystack.IndexOf("WorkItem", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Global activity query returned an event that does not match WorkItem."
    }
}

Write-Host ""
Write-Host "Querying global activity by source MigrationRuns..."
$runEvents = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/activity/query?source=MigrationRuns&limit=10" `
    -ContentType "application/json"

$runEvents | ConvertTo-Json -Depth 20

foreach ($event in @($runEvents.events)) {
    if ($event.source -ne "MigrationRuns") {
        throw "Global activity source filter returned a non-MigrationRuns event."
    }
}

Write-Host ""
Write-Host "Global operational activity query smoke passed."
