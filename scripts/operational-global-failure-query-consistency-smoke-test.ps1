param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$Limit = 10
)

$ErrorActionPreference = "Stop"

Write-Host "Loading recent failures feed..."
$recent = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/recent?limit=500" `
    -ContentType "application/json"

Write-Host "Loading unfiltered failure query..."
$query = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/query?limit=$Limit" `
    -ContentType "application/json"

if ($query.limit -ne $Limit) {
    throw "Failure query limit does not match requested limit."
}

if ($query.count -ne @($query.failures).Count) {
    throw "Failure query count does not match failures array length."
}

if ($query.count -gt $Limit) {
    throw "Failure query returned more failures than requested limit."
}

$recentKeys = @{}
foreach ($failure in @($recent.failures)) {
    $key = "$($failure.failureId)|$($failure.runId)|$($failure.createdAt)|$($failure.failureType)|$($failure.message)"
    $recentKeys[$key] = $true
}

foreach ($failure in @($query.failures)) {
    $key = "$($failure.failureId)|$($failure.runId)|$($failure.createdAt)|$($failure.failureType)|$($failure.message)"

    if (-not $recentKeys.ContainsKey($key)) {
        throw "Failure query returned a failure not present in the recent failures sample."
    }
}

Write-Host ""
Write-Host "Loading retriable failure query..."
$retriable = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/query?isRetriable=true&limit=$Limit" `
    -ContentType "application/json"

foreach ($failure in @($retriable.failures)) {
    if (-not $failure.isRetriable) {
        throw "Retriable filter returned a non-retriable failure."
    }
}

Write-Host ""
Write-Host "Loading search failure query..."
$search = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/query?q=Failure&limit=$Limit" `
    -ContentType "application/json"

foreach ($failure in @($search.failures)) {
    $haystack = "$($failure.failureId) $($failure.runId) $($failure.manifestRecordId) $($failure.workItemId) $($failure.failureType) $($failure.message) $($failure.details) $($failure.runStatus) $($failure.sourceSystem) $($failure.targetSystem) $($failure.workItemStatus)"

    if ($haystack.IndexOf("Failure", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Search failure query returned a failure that does not match search text."
    }
}

Write-Host "BaseQueryCount: $($query.count)"
Write-Host "RetriableQueryCount: $($retriable.count)"
Write-Host "SearchQueryCount: $($search.count)"

Write-Host ""
Write-Host "Global operational failure query consistency smoke passed."
