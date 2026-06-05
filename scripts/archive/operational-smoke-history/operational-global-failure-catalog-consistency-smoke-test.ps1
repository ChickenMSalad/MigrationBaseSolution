param(
    [string]$BaseUrl = "https://localhost:55436",
    [int]$SampleLimit = 100
)

$ErrorActionPreference = "Stop"

Write-Host "Loading recent failures sample..."
$recent = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/recent?limit=$SampleLimit" `
    -ContentType "application/json"

Write-Host "Loading failure catalog..."
$catalog = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/failures/catalog?sampleLimit=$SampleLimit" `
    -ContentType "application/json"

$failures = @($recent.failures)

$expectedFailureTypes = @($failures | ForEach-Object { $_.failureType } | Where-Object { $_ } | Sort-Object -Unique)
$expectedRunStatuses = @($failures | ForEach-Object { $_.runStatus } | Where-Object { $_ } | Sort-Object -Unique)
$expectedSourceSystems = @($failures | ForEach-Object { $_.sourceSystem } | Where-Object { $_ } | Sort-Object -Unique)
$expectedTargetSystems = @($failures | ForEach-Object { $_.targetSystem } | Where-Object { $_ } | Sort-Object -Unique)

$actualFailureTypes = @($catalog.failureTypes | Sort-Object -Unique)
$actualRunStatuses = @($catalog.runStatuses | Sort-Object -Unique)
$actualSourceSystems = @($catalog.sourceSystems | Sort-Object -Unique)
$actualTargetSystems = @($catalog.targetSystems | Sort-Object -Unique)

if ($catalog.failureTypeCount -ne $actualFailureTypes.Count) {
    throw "Catalog failureTypeCount does not match failureTypes length."
}

if ($catalog.runStatusCount -ne $actualRunStatuses.Count) {
    throw "Catalog runStatusCount does not match runStatuses length."
}

if ($catalog.sourceSystemCount -ne $actualSourceSystems.Count) {
    throw "Catalog sourceSystemCount does not match sourceSystems length."
}

if ($catalog.targetSystemCount -ne $actualTargetSystems.Count) {
    throw "Catalog targetSystemCount does not match targetSystems length."
}

if (($expectedFailureTypes -join "|") -ne ($actualFailureTypes -join "|")) {
    throw "Catalog failureTypes do not match recent failures sample."
}

if (($expectedRunStatuses -join "|") -ne ($actualRunStatuses -join "|")) {
    throw "Catalog runStatuses do not match recent failures sample."
}

if (($expectedSourceSystems -join "|") -ne ($actualSourceSystems -join "|")) {
    throw "Catalog sourceSystems do not match recent failures sample."
}

if (($expectedTargetSystems -join "|") -ne ($actualTargetSystems -join "|")) {
    throw "Catalog targetSystems do not match recent failures sample."
}

Write-Host "FailureTypeCount: $($catalog.failureTypeCount)"
Write-Host "RunStatusCount: $($catalog.runStatusCount)"
Write-Host "SourceSystemCount: $($catalog.sourceSystemCount)"
Write-Host "TargetSystemCount: $($catalog.targetSystemCount)"

Write-Host ""
Write-Host "Global operational failure catalog consistency smoke passed."
