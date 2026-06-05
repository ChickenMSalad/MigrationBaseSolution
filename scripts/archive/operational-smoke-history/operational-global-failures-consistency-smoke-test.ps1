param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$Limit = 25
)

$ErrorActionPreference = "Stop"

Write-Host "Loading recent failures..."
$failures = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/recent?limit=$Limit" `
    -ContentType "application/json"

Write-Host "Loading recent activity feed..."
$activity = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/activity/recent?limit=500" `
    -ContentType "application/json"

if ($failures.count -ne @($failures.failures).Count) {
    throw "Failures count does not match failures array length."
}

if ($failures.count -gt $Limit) {
    throw "Failures endpoint returned more items than requested limit."
}

$failureItems = @($failures.failures)

for ($i = 1; $i -lt $failureItems.Count; $i++) {
    $previous = [DateTimeOffset]$failureItems[$i - 1].createdAt
    $current = [DateTimeOffset]$failureItems[$i].createdAt

    if ($current -gt $previous) {
        throw "Failures are not sorted descending by createdAt."
    }
}

$activityEvents = @($activity.events)

foreach ($failure in $failureItems) {
    if (-not $failure.failureId) {
        throw "Failure item is missing failureId."
    }

    if (-not $failure.runId) {
        throw "Failure item is missing runId."
    }

    if (-not $failure.failureType) {
        throw "Failure item is missing failureType."
    }

    if (-not $failure.message) {
        throw "Failure item is missing message."
    }

    $matchingActivity = $activityEvents | Where-Object {
        $_.failureId -eq $failure.failureId
    }

    if (@($matchingActivity).Count -eq 0) {
        Write-Warning "No matching activity event found for failure $($failure.failureId)."
    }
}

Write-Host "FailureCount: $($failures.count)"
Write-Host "ActivityEventCount: $($activity.eventCount)"

Write-Host ""
Write-Host "Global operational failures consistency smoke passed."
